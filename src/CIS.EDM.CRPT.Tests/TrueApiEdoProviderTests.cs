using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CIS.Cryptography;
using Spoleto.TrueApi.Edo.Enums;
using Spoleto.TrueApi.Edo.Models;
using Spoleto.TrueApi.Edo.Models.Options;
using Spoleto.TrueApi.Edo.Models.Upd;
using Spoleto.TrueApi.Edo.Providers;

#nullable enable

namespace CIS.EDM.CRPT.Tests
{
    /// <summary>
    /// Вызовы стенда True API: нужны сертификат и настройки <see cref="TestOption"/>.
    /// </summary>
    public class TrueApiEdoProviderTests
    {
        private readonly TestOption _testOption = ConfigurationHelper.GetTestOption();

        static TrueApiEdoProviderTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//support for "windows-1251"
        }

        private TrueApiEdoProviderOption CreateSettings()
        {
            var certificateList = CryptographyHelper.GetCertificates(true, true);
            var cert = certificateList.Single(x => x.Certificate.Thumbprint.ToLower() == _testOption.CertificateThumbprint);

            return new TrueApiEdoProviderOption
            {
                CertificateThumbprint = cert.Certificate.Thumbprint,
                ServiceUrl = _testOption.ServiceUrl,
                Inn = _testOption.Inn
            };
        }

        private static string FindDocumentId(EdoDocumentCollection list, string direction, Func<EdoDocumentType, bool> isSuitable, string suitableTypes)
        {
            var documents = list.Items.SelectMany(x => x.Documents).ToList();
            foreach (var document in documents)
            {
                TestContext.Out.WriteLine($"{(int)document.Type} {document.Type} {document.Number} {document.Id}");
            }

            var documentId = documents.FirstOrDefault(x => isSuitable(x.Type))?.Id;
            if (documentId == null)
            {
                Assert.Ignore($"Среди {documents.Count} {direction} документов на стенде нет {suitableTypes}.");
            }

            return documentId!;
        }

        /// <summary>
        /// Договоры с оператором ГИС МТ хранятся в PDF, поэтому берём универсальный документ: УПД, УКД или квитанцию, они всегда XML.
        /// </summary>
        private static string FindUniversalDocumentId(EdoDocumentCollection list, string direction)
            => FindDocumentId(list, direction, x => Enum.IsDefined(x), "УПД, УКД и квитанций");

        /// <summary>
        /// ZIP-архив с подписями есть только у УПД и УКД. Берём УПД или УПДи: в их архиве информация продавца находится и без имени архива.
        /// </summary>
        private static string FindUpdDocumentId(EdoDocumentCollection list, string direction)
            => FindDocumentId(list, direction,
                x => x is >= EdoDocumentType.UPDWithDOPWaybill and <= EdoDocumentType.UPD970WithSCHFDOPBuyerTitleInvoiceWaybill
                    or >= EdoDocumentType.UPDiWithDOPWaybillCorrected and <= EdoDocumentType.UPDi970WithSCHFDOPBuyerTitleInvoiceCorrectedWaybill,
                "УПД и УПДи");

        /// <summary>
        /// Подписать можно только УПД и УПДи формата 970: подписание УПД версии 5.01 сервис отклоняет с 31.03.2025.
        /// </summary>
        private static string FindUpd970DocumentId(EdoDocumentCollection list, string direction)
            => FindDocumentId(list, direction,
                x => x is >= EdoDocumentType.UPD970WithDOPWaybill and <= EdoDocumentType.UPD970WithSCHFDOPBuyerTitleInvoiceWaybill
                    or >= EdoDocumentType.UPDi970WithDOPWaybillCorrected and <= EdoDocumentType.UPDi970WithSCHFDOPBuyerTitleInvoiceCorrectedWaybill,
                "УПД и УПДи формата 970");

        /// <summary>
        /// Кодировку берём из заголовка XML (у УПД windows-1251), а не угадываем: так читаются и УПД, и квитанции.
        /// Корень всех форматов ФНС - «Файл», поэтому кириллица в нём проверяет, что байты пришли без порчи.
        /// </summary>
        private static void AssertFnsXml(byte[] content)
        {
            var xml = XDocument.Load(new MemoryStream(content));
            TestContext.Out.WriteLine(xml.Root?.Attribute("ИдФайл")?.Value);
            Assert.That(xml.Root?.Name.LocalName, Is.EqualTo("Файл"));
        }

        /// <summary>
        /// Имя файла в архиве повторяет ИдФайл. Подпись - CMS в DER: первый байт 0x30 (SEQUENCE), а не Base64-текст.
        /// </summary>
        private static void AssertSignedFnsXml(EdoSignedDocumentInfo document)
        {
            TestContext.Out.WriteLine($"{document.FileName}: файл {document.Content.Length} байт, подпись {document.Signature.Length} байт");
            AssertFnsXml(document.RawContent);

            var xml = XDocument.Parse(document.Content);
            Assert.Multiple(() =>
            {
                Assert.That(xml.Root?.Name.LocalName, Is.EqualTo("Файл"));
                Assert.That(xml.Root?.Attribute("ИдФайл")?.Value, Is.EqualTo(document.FileName));
                Assert.That(document.Signature, Is.Not.Empty);
                Assert.That(document.Signature[0], Is.EqualTo(0x30));
            });
        }

        /// <summary>
        /// ИдФайл информации продавца УПД начинается с ON_NSCHFDOPPR, даты - строки ДД.ММ.ГГГГ, как в файле.
        /// </summary>
        private static void AssertSellerDocumentInfo(EdoSellerDocumentInfo info)
        {
            TestContext.Out.WriteLine($"{info.FileId}: {info.Function} № {info.DocumentNumber} от {info.DocumentDate}, УПДи: {info.IsUPDi}, УИД: {info.DocumentUid}");

            Assert.Multiple(() =>
            {
                Assert.That(info.FileId, Does.StartWith("ON_NSCHFDOPPR"));
                Assert.That(info.DateCreation, Does.Match(@"^\d{2}\.\d{2}\.\d{4}$"));
                Assert.That(info.DocumentDate, Does.Match(@"^\d{2}\.\d{2}\.\d{4}$"));
                Assert.That(info.Signature, Is.Not.Empty);
            });
        }

        [Test]
        public async Task GetIncomingDocumentListTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            // Act
            var data = await edoProvider.GetIncomingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 10 });

            // Assert
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Items, Has.Count.LessThanOrEqualTo(10));
        }

        [Test]
        public async Task GetIncomingDocumentTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            var list = await edoProvider.GetIncomingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 50 });
            var documentId = FindUniversalDocumentId(list, "входящих");

            // Act
            var content = await edoProvider.GetIncomingDocumentAsync(settings, documentId);

            // Assert
            AssertFnsXml(content);
        }

        [Test]
        public async Task GetOutgoingDocumentListTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            // Act
            var data = await edoProvider.GetOutgoingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 10 });

            // Assert
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Items, Has.Count.LessThanOrEqualTo(10));
        }

        [Test]
        public async Task GetOutgoingDocumentTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            var list = await edoProvider.GetOutgoingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 50 });
            var documentId = FindUniversalDocumentId(list, "исходящих");

            // Act
            var content = await edoProvider.GetOutgoingDocumentAsync(settings, documentId);

            // Assert
            AssertFnsXml(content);
        }

        [Test]
        public async Task GetIncomingSignedDocumentInfoTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            var list = await edoProvider.GetIncomingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 50 });
            var documentId = FindUpdDocumentId(list, "входящих");

            // Act
            var document = await edoProvider.GetIncomingSignedDocumentInfoAsync(settings, documentId);

            // Assert
            AssertSignedFnsXml(document);
        }

        [Test]
        public async Task GetOutgoingSignedDocumentInfoTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            var list = await edoProvider.GetOutgoingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 50 });
            var documentId = FindUpdDocumentId(list, "исходящих");

            // Act
            var document = await edoProvider.GetOutgoingSignedDocumentInfoAsync(settings, documentId);

            // Assert
            AssertSignedFnsXml(document);
        }

        [Test]
        public async Task GetIncomingSellerDocumentInfoTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            var list = await edoProvider.GetIncomingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 50 });
            var documentId = FindUpdDocumentId(list, "входящих");

            // Act
            var info = await edoProvider.GetIncomingSellerDocumentInfoAsync(settings, documentId);

            // Assert
            AssertSellerDocumentInfo(info);
        }

        [Test]
        public async Task GetOutgoingSellerDocumentInfoTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            var list = await edoProvider.GetOutgoingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 50 });
            var documentId = FindUpdDocumentId(list, "исходящих");

            // Act
            var info = await edoProvider.GetOutgoingSellerDocumentInfoAsync(settings, documentId);

            // Assert
            AssertSellerDocumentInfo(info);
        }

        /// <summary>
        /// Владелец сертификата из настроек - подписант документа: фамилия из SN, имя и отчество из G.
        /// </summary>
        private EdoPerson GetCertificateOwner()
        {
            var subject = CryptographyHelper.GetCertificates(true, true).Single(x => x.Certificate.Thumbprint.ToLower() == _testOption.CertificateThumbprint).Certificate.SubjectName;
            TestContext.Out.WriteLine(subject);

            var givenName = GetSubjectValue(subject, "G")?.Split(' ', 2);
            return new EdoPerson
            {
                Surname = GetSubjectValue(subject, "SN") ?? "Тестов",
                FirstName = givenName?[0] ?? "Тест",
                Patronymic = givenName?.Length > 1 ? givenName[1] : null
            };
        }

        private static string? GetSubjectValue(string subject, string key)
        {
            var match = Regex.Match(subject, $@"(?:^|,\s*){key}=(""[^""]*""|[^,]*)");
            return match.Success ? match.Groups[1].Value.Trim('"') : null;
        }

        private static EdoOrganization ReadLegalPerson(XElement invoice, string elementName)
        {
            var legalPerson = invoice.Element(elementName)?.Element("ИдСв")?.Element("СвЮЛУч");
            if (legalPerson == null)
            {
                Assert.Ignore($"В УПД на стенде {elementName} - не юридическое лицо.");
            }

            return new EdoOrganization
            {
                OrganizationIdentificationInfo = new()
                {
                    LegalPerson = new() { Name = legalPerson!.Attribute("НаимОрг")!.Value, Inn = legalPerson.Attribute("ИННЮЛ")!.Value, Kpp = legalPerson.Attribute("КПП")?.Value }
                },
                Address = new() { RussianAddress = new() { RegionCode = "77", Region = "г. Москва" } }
            };
        }

        private const string MarkCodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        /// <summary>
        /// Случайный код маркировки: 01 + GTIN + 21 + серийный номер из 13 символов.
        /// </summary>
        private static string CreateRandomMarkCode()
            => "010465029413145221" + new string(Random.Shared.GetItems(MarkCodeAlphabet.AsSpan(), 13));

        /// <summary>
        /// УПД на стенде: идентификаторы ЭДО и стороны берутся из уже отправленного УПД (ИдФайл ON_NSCHFDOPPR_{получатель}_{отправитель}_…), подписант - владелец сертификата.
        /// </summary>
        /// <remarks>
        /// Строка с кодом маркировки делает УПД маркированным (N3 = 1 в имени файла): после титула покупателя такой документ уходит в ГИС МТ,
        /// статус «Подписан: Отправлен в ГИС МТ». УПД без кодов остаётся в статусе «Подписан». Код каждый раз новый, чтобы УПД на стенде не повторяли его.
        /// </remarks>
        private async Task<EdoSellerUniversalTransferDocument> CreateStandUniversalTransferDocumentAsync(TrueApiEdoProvider edoProvider, TrueApiEdoProviderOption settings)
        {
            var list = await edoProvider.GetOutgoingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 50 });
            var documentId = FindUpdDocumentId(list, "исходящих");
            var file = XDocument.Load(new MemoryStream(await edoProvider.GetOutgoingDocumentAsync(settings, documentId))).Root!;

            var fileIdParts = file.Attribute("ИдФайл")!.Value.Split('_');
            var invoice = file.Element("Документ")!.Element("СвСчФакт")!;
            var seller = ReadLegalPerson(invoice, "СвПрод");
            var now = DateTime.UtcNow;

            return new EdoSellerUniversalTransferDocument
            {
                SenderEdoId = fileIdParts[3],
                RecipientEdoId = fileIdParts[2],
                Function = EdoDocumentFunction.InvoiceAndPrimaryDocument,
                DocumentNumber = $"Т-{now:yyyyMMddHHmmss}",
                DocumentDate = now.Date,
                DocumentCreator = seller,
                Sellers = [seller],
                Buyers = [ReadLegalPerson(invoice, "СвПокуп")],
                Items =
                [
                    new()
                    {
                        ProductName = "Тестовый товар",
                        UnitCode = "796",
                        UnitName = "шт",
                        Quantity = 1,
                        Price = 100,
                        SumWithoutVat = 100,
                        TaxRate = EdoTaxRate.TwentyTwoPercent,
                        Vat = 22,
                        Sum = 122,
                        AdditionalInfo = new() { ItemIdentificationNumberList = [new() { MarkItems = [CreateRandomMarkCode()] }] }
                    }
                ],
                TransferInfo = new() { TransferDetails = new() { Date = now.Date } },
                Signers = [new() { JobTitle = "Директор", AuthorityConfirmationType = EdoAuthorityConfirmationType.ElectronicSignatureData, Person = GetCertificateOwner() }]
            };
        }

        /// <summary>
        /// Загружает черновик УПД формата 970. Его потом подписывает <see cref="SignOutgoingDocumentTest"/>.
        /// </summary>
        [Test]
        [Explicit("Создаёт черновик УПД на стенде.")]
        public async Task PostUniversalTransferDocumentTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();
            var document = await CreateStandUniversalTransferDocumentAsync(edoProvider, settings);

            // Act
            var result = await edoProvider.PostUniversalTransferDocumentAsync(settings, document, isDraft: true);

            // Assert
            TestContext.Out.WriteLine(result);
            var content = await edoProvider.GetOutgoingDocumentAsync(settings, result.Id);
            Assert.That(XDocument.Load(new MemoryStream(content)).Root!.Attribute("ИдФайл")!.Value, Is.EqualTo(result.FileId));
        }

        /// <summary>
        /// Подписывает самый новый черновик исходящего УПД / УПДи формата 970 сертификатом из настроек, и тот уходит получателю.
        /// </summary>
        [Test]
        [Explicit("Отправляет черновик получателю: на стенде одним черновиком становится меньше.")]
        public async Task SignOutgoingDocumentTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            // Новые черновики первыми: старые на стенде могут быть в версии 5.01.
            var draftSearchModel = new EdoDocumentSearchModel { Limit = 50, Status = EdoDocumentStatus.Draft, SortBy = EdoDocumentSortField.CreatedAt };
            var drafts = await edoProvider.GetOutgoingDocumentListAsync(settings, draftSearchModel);
            var documentId = FindUpd970DocumentId(drafts, "черновиков исходящих");

            // Act
            await edoProvider.SignOutgoingDocumentAsync(settings, documentId);

            // Assert
            var draftsAfterSigning = await edoProvider.GetOutgoingDocumentListAsync(settings, draftSearchModel);
            Assert.That(draftsAfterSigning.Items.SelectMany(x => x.Documents).Select(x => x.Id), Does.Not.Contain(documentId));
        }

        /// <summary>
        /// Входящие УПД и УПДи формата 970, которые ждут титула покупателя: подписи ждут статусы 3 (доставлен) и 13 (просмотрен).
        /// Титул покупателя бывает только у функций ДОП и СЧФДОП.
        /// </summary>
        private static async Task<EdoDocumentCollection> GetDocumentsAwaitingBuyerTitleAsync(TrueApiEdoProvider edoProvider, TrueApiEdoProviderOption settings)
        {
            var searchModel = new EdoDocumentSearchModel { Limit = 50, SortBy = EdoDocumentSortField.CreatedAt };
            var delivered = await edoProvider.GetIncomingDocumentListAsync(settings, searchModel with { Status = EdoDocumentStatus.DeliveredSignatureRequired });
            var reviewed = await edoProvider.GetIncomingDocumentListAsync(settings, searchModel with { Status = EdoDocumentStatus.ReviewedSignatureRequired });

            // Каждый список отсортирован сервисом сам по себе: после объединения сортируем заново, новые первыми.
            return new EdoDocumentCollection { Items = [.. delivered.Items.Concat(reviewed.Items).OrderByDescending(x => x.CreatedAt)] };
        }

        private static readonly EdoDocumentType[] BuyerTitleDocumentTypes =
        [
            EdoDocumentType.UPD970WithDOPWaybill,
            EdoDocumentType.UPD970WithSCHFDOPInvoiceWaybill,
            EdoDocumentType.UPDi970WithDOPWaybillCorrected,
            EdoDocumentType.UPDi970WithSCHFDOPInvoiceCorrectedWaybill
        ];

        /// <summary>
        /// Подписывает входящий УПД / УПДи формата 970, который ждёт подписи: «принято без расхождений», покупатель - из файла продавца, подписант - владелец сертификата.
        /// </summary>
        [Test]
        [Explicit("Отправляет титул покупателя продавцу: входящий УПД на стенде становится подписанным.")]
        public async Task ReceiptUniversalTransferDocumentTest()
        {
            // Arrange
            var settings = CreateSettings();
            using var edoProvider = new TrueApiEdoProvider();

            var awaiting = await GetDocumentsAwaitingBuyerTitleAsync(edoProvider, settings);
            var documentId = FindDocumentId(awaiting, "ждущих подписи входящих", BuyerTitleDocumentTypes.Contains, "УПД и УПДи формата 970 с функцией ДОП или СЧФДОП");

            var file = XDocument.Load(new MemoryStream(await edoProvider.GetIncomingDocumentAsync(settings, documentId))).Root!;
            var buyer = ReadLegalPerson(file.Element("Документ")!.Element("СвСчФакт")!, "СвПокуп");
            var document = new EdoBuyerUniversalTransferDocument
            {
                DocumentId = documentId,
                DocumentCreator = buyer,
                AcceptanceInfo = new() { OperationName = "Товары приняты без претензий", Date = DateTime.UtcNow.Date },
                Signers = [new() { JobTitle = "Директор", AuthorityConfirmationType = EdoAuthorityConfirmationType.ElectronicSignatureData, Person = GetCertificateOwner() }]
            };

            // Act
            var result = await edoProvider.ReceiptUniversalTransferDocumentAsync(settings, document);

            // Assert
            TestContext.Out.WriteLine(result);
            TestContext.Out.WriteLine(result.Content);
            var awaitingAfterTitle = await GetDocumentsAwaitingBuyerTitleAsync(edoProvider, settings);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.Not.Empty);
                Assert.That(awaitingAfterTitle.Items.SelectMany(x => x.Documents).Select(x => x.Id), Does.Not.Contain(documentId));
            });
        }
    }
}
