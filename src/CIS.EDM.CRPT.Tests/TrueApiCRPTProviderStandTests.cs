using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CIS.Cryptography;
using CIS.EDM.CRPT.Helpers;
using CIS.EDM.CRPT.Models;
using CIS.EDM.CRPT.Providers;
using CIS.EDM.Models;
using CIS.EDM.Models.Reference;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Spoleto.TrueApi.Edo.Enums;
using VB = CIS.EDM.Models.V5_03.Buyer;
using VR = CIS.EDM.Models.V5_03.Reference;
using VS = CIS.EDM.Models.V5_03.Seller;

namespace CIS.EDM.CRPT.Tests
{
    /// <summary>
    /// Вызовы <see cref="TrueApiCRPTProvider"/> на стенде True API: нужны сертификат и настройки <see cref="TestOption"/>.
    /// </summary>
    /// <remarks>
    /// Те же вызовы, что у TrueApiEdoProviderTests в Spoleto.TrueApi, но через модели CIS.EDM, как их вызывает приложение.
    /// Тесты, которые меняют документы на стенде, запускаются только явно (<see cref="ExplicitAttribute"/>).
    /// </remarks>
    public class TrueApiCRPTProviderStandTests
    {
        private readonly TestOption _testOption = ConfigurationHelper.GetTestOption();

        // Клиенты, как в приложении, - из IHttpClientFactory.
        private static readonly IHttpClientFactory _httpClientFactory = new ServiceCollection().AddHttpClient().BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        static TrueApiCRPTProviderStandTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//support for "windows-1251"
        }

        private static TrueApiCRPTProvider CreateProvider() => new(NullLogger<TrueApiCRPTProvider>.Instance, _httpClientFactory);

        /// <summary>
        /// Сертификат из настроек: отпечаток для подписи и владелец для подписанта.
        /// </summary>
        private (string Thumbprint, string Subject) GetCertificate()
        {
            var certificate = CryptographyHelper.GetCertificates(true, true)
                .Single(x => string.Equals(x.Certificate.Thumbprint, _testOption.CertificateThumbprint, StringComparison.OrdinalIgnoreCase)).Certificate;

            return (certificate.Thumbprint, certificate.SubjectName);
        }

        /// <summary>
        /// Адрес True API провайдер берёт из <see cref="CRPTOption.AuthUrl"/>, <see cref="CRPTOption.ServiceUrl"/> ему не нужен.
        /// </summary>
        private CRPTOption CreateSettings() => new()
        {
            AuthUrl = _testOption.ServiceUrl,
            CertificateThumbprint = GetCertificate().Thumbprint,
            Inn = _testOption.Inn
        };

        private static string FindDocumentId(DocumentCollection list, string direction, Func<EdoDocumentType, bool> isSuitable, string suitableTypes)
        {
            var documents = list.Items.SelectMany(x => x.Documents).ToList();
            foreach (var document in documents)
            {
                TestContext.Out.WriteLine($"{(int)document.Type} {document.Type} {document.Number} {document.Id}");
            }

            // Коды типов в CIS.EDM и True API одни и те же, поэтому типы отбираются, как в тестах Spoleto.TrueApi.
            var documentId = documents.FirstOrDefault(x => isSuitable((EdoDocumentType)(int)x.Type))?.Id;
            if (documentId == null)
            {
                Assert.Ignore($"Среди {documents.Count} {direction} документов на стенде нет {suitableTypes}.");
            }

            return documentId;
        }

        /// <summary>
        /// Договоры с оператором ГИС МТ хранятся в PDF, поэтому берём универсальный документ: УПД, УКД или квитанцию, они всегда XML.
        /// </summary>
        private static bool IsUniversalDocument(EdoDocumentType type) => Enum.IsDefined(type);

        /// <summary>
        /// ZIP-архив с подписями есть только у УПД и УКД. Берём УПД или УПДи: в их архиве информация продавца находится и без имени архива.
        /// </summary>
        private static bool IsUpd(EdoDocumentType type) => IsUpd820(type) || IsUpd970(type);

        private static bool IsUpd820(EdoDocumentType type)
            => type is >= EdoDocumentType.UPDWithDOPWaybill and <= EdoDocumentType.UPDWithSCHFDOPBuyerInformationInvoiceWaybill
                or >= EdoDocumentType.UPDiWithDOPWaybillCorrected and <= EdoDocumentType.UPDiWithSCHFDOPBuyerInformationInvoiceCorrectedWaybill;

        /// <summary>
        /// Подписать можно только УПД и УПДи формата 970: подписание УПД версии 5.01 сервис отклоняет с 31.03.2025.
        /// </summary>
        private static bool IsUpd970(EdoDocumentType type)
            => type is >= EdoDocumentType.UPD970WithDOPWaybill and <= EdoDocumentType.UPD970WithSCHFDOPBuyerTitleInvoiceWaybill
                or >= EdoDocumentType.UPDi970WithDOPWaybillCorrected and <= EdoDocumentType.UPDi970WithSCHFDOPBuyerTitleInvoiceCorrectedWaybill;

        /// <summary>
        /// Корень всех форматов ФНС - «Файл», поэтому кириллица в нём проверяет, что файл прочитан в правильной кодировке.
        /// </summary>
        private static XElement AssertFnsXml(string content)
        {
            var root = XDocument.Parse(content).Root;
            TestContext.Out.WriteLine(root?.Attribute("ИдФайл")?.Value);
            Assert.That(root?.Name.LocalName, Is.EqualTo("Файл"));

            return root;
        }

        /// <summary>
        /// Подпись - CMS в DER: после Base64 первый байт 0x30 (SEQUENCE).
        /// </summary>
        private static void AssertSignedFnsXml(SignedDocumentInfo document)
        {
            var signature = Convert.FromBase64String(document.DetachedSignature);
            TestContext.Out.WriteLine($"Файл {document.Content.Length} символов, подпись {signature.Length} байт");

            AssertFnsXml(document.Content);
            Assert.That(signature[0], Is.EqualTo(0x30));
        }

        [Test]
        public async Task GetIncomingDocumentListTest()
        {
            // Arrange
            var settings = CreateSettings();
            var provider = CreateProvider();

            // Act
            var data = await provider.GetIncomingDocumentListAsync(settings, new DocumentCollectionSearchModel { Limit = 10 });

            // Assert
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Items, Has.Count.LessThanOrEqualTo(10));
        }

        /// <summary>
        /// Несколько статусов - несколько запросов True API, ответы объединяются: в списке только эти статусы, без повторов, новые первыми.
        /// </summary>
        [Test]
        public async Task GetIncomingDocumentList_SeveralStatusesTest()
        {
            // Arrange
            var settings = CreateSettings();
            var provider = CreateProvider();
            DocumentState[] statuses = [DocumentState.DeliveredSignatureRequired, DocumentState.ReviewedSignatureRequired, DocumentState.Signed];

            // Act
            var data = await provider.GetIncomingDocumentListAsync(settings, new DocumentCollectionSearchModel { Limit = 10, Status = [.. statuses] });

            // Assert
            foreach (var document in data.Items)
            {
                TestContext.Out.WriteLine($"{document.CreatedAt:O} {(int)document.Status} {document.Status} {document.Number}");
            }

            Assert.Multiple(() =>
            {
                Assert.That(data.Items.Select(x => x.Status), Is.SubsetOf(statuses));
                Assert.That(data.Items.Select(x => x.Id), Is.Unique);
                Assert.That(data.Items.Select(x => x.CreatedAt), Is.Ordered.Descending);
            });
        }

        [Test]
        public async Task GetIncomingDocumentTest()
        {
            // Arrange
            var settings = CreateSettings();
            var provider = CreateProvider();

            var list = await provider.GetIncomingDocumentListAsync(settings, new DocumentCollectionSearchModel { Limit = 50 });
            var documentId = FindDocumentId(list, "входящих", IsUniversalDocument, "УПД, УКД и квитанций");

            // Act
            var content = await provider.GetIncomingDocumentAsync(settings, documentId);

            // Assert
            AssertFnsXml(content);
        }

        [Test]
        public async Task GetOutgoingDocumentListTest()
        {
            // Arrange
            var settings = CreateSettings();
            var provider = CreateProvider();

            // Act
            var data = await provider.GetOutgoingDocumentListAsync(settings, new DocumentCollectionSearchModel { Limit = 10 });

            // Assert
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Items, Has.Count.LessThanOrEqualTo(10));
        }

        [Test]
        public async Task GetOutgoingDocumentTest()
        {
            // Arrange
            var settings = CreateSettings();
            var provider = CreateProvider();

            var list = await provider.GetOutgoingDocumentListAsync(settings, new DocumentCollectionSearchModel { Limit = 50 });
            var documentId = FindDocumentId(list, "исходящих", IsUniversalDocument, "УПД, УКД и квитанций");

            // Act
            var content = await provider.GetOutgoingDocumentAsync(settings, documentId);

            // Assert
            AssertFnsXml(content);
        }

        [Test]
        public async Task GetIncomingSignedDocumentInfoTest()
        {
            // Arrange
            var settings = CreateSettings();
            var provider = CreateProvider();

            var list = await provider.GetIncomingDocumentListAsync(settings, new DocumentCollectionSearchModel { Limit = 50 });
            var documentId = FindDocumentId(list, "входящих", IsUpd, "УПД и УПДи");

            // Act
            var document = await provider.GetIncomingSignedDocumentInfoAsync(settings, documentId);

            // Assert
            AssertSignedFnsXml(document);
        }

        [Test]
        public async Task GetOutgoingSignedDocumentInfoTest()
        {
            // Arrange
            var settings = CreateSettings();
            var provider = CreateProvider();

            var list = await provider.GetOutgoingDocumentListAsync(settings, new DocumentCollectionSearchModel { Limit = 50 });
            var documentId = FindDocumentId(list, "исходящих", IsUpd, "УПД и УПДи");

            // Act
            var document = await provider.GetOutgoingSignedDocumentInfoAsync(settings, documentId);

            // Assert
            AssertSignedFnsXml(document);
        }

        /// <summary>
        /// Сведения о файле продавца и они же, разобранные из того же архива старым <see cref="XmlParserV5_03"/>, как их получал <see cref="CRPTProvider"/>.
        /// </summary>
        /// <remarks>
        /// Только УПД формата 970: файл формата 820 Spoleto.TrueApi.Edo не читает (нет ДатаДок), а для старых документов приложение сведения не запрашивает.
        /// </remarks>
        private async Task<(SellerDocumentInfo Actual, SellerDocumentInfo Expected)> GetSellerDocumentInfoAsync(bool incoming, Func<EdoDocumentType, bool> isSuitable, string suitableTypes)
        {
            var settings = CreateSettings();
            var provider = CreateProvider();
            var searchModel = new DocumentCollectionSearchModel { Limit = 50 };

            var list = incoming
                ? await provider.GetIncomingDocumentListAsync(settings, searchModel)
                : await provider.GetOutgoingDocumentListAsync(settings, searchModel);
            var documentId = FindDocumentId(list, incoming ? "входящих" : "исходящих", isSuitable, suitableTypes);

            var signedDocument = incoming
                ? await provider.GetIncomingSignedDocumentInfoAsync(settings, documentId)
                : await provider.GetOutgoingSignedDocumentInfoAsync(settings, documentId);
            var expected = XmlParserV5_03.SellerInfoFromXml(signedDocument);

            var actual = incoming
                ? await provider.GetIncomingSellerDocumentInfoAsync(settings, documentId)
                : await provider.GetOutgoingSellerDocumentInfoAsync(settings, documentId);
            TestContext.Out.WriteLine($"{actual.FileId}: {actual.Function} № {actual.DocumentNumber} от {actual.DocumentDate}, УПДи: {actual.IsUPDi}, УИД: {actual.DocumentUid}");

            return (actual, expected);
        }

        [Test]
        public async Task GetIncomingSellerDocumentInfoTest()
        {
            // Arrange, Act
            var (info, expected) = await GetSellerDocumentInfoAsync(true, IsUpd970, "УПД и УПДи формата 970");

            // Assert
            Assert.That(JsonSerializer.Serialize(info), Is.EqualTo(JsonSerializer.Serialize(expected)));
        }

        [Test]
        public async Task GetOutgoingSellerDocumentInfoTest()
        {
            // Arrange, Act
            var (info, expected) = await GetSellerDocumentInfoAsync(false, IsUpd970, "УПД и УПДи формата 970");

            // Assert
            Assert.That(JsonSerializer.Serialize(info), Is.EqualTo(JsonSerializer.Serialize(expected)));
        }

        /// <summary>
        /// Владелец сертификата из настроек - подписант документа: фамилия из SN, имя и отчество из G.
        /// </summary>
        private (string Surname, string FirstName, string Patronymic) GetCertificateOwner()
        {
            var subject = GetCertificate().Subject;
            TestContext.Out.WriteLine(subject);

            var givenName = GetSubjectValue(subject, "G")?.Split(' ', 2);
            return (GetSubjectValue(subject, "SN") ?? "Тестов", givenName?[0] ?? "Тест", givenName?.Length > 1 ? givenName[1] : null);
        }

        private static string GetSubjectValue(string subject, string key)
        {
            var match = Regex.Match(subject, $@"(?:^|,\s*){key}=(""[^""]*""|[^,]*)");
            return match.Success ? match.Groups[1].Value.Trim('"') : null;
        }

        private static VS.Organization ReadLegalPerson(XElement invoice, string elementName)
        {
            var legalPerson = invoice.Element(elementName)?.Element("ИдСв")?.Element("СвЮЛУч");
            if (legalPerson == null)
            {
                Assert.Ignore($"В УПД на стенде {elementName} - не юридическое лицо.");
            }

            return new VS.Organization
            {
                OrganizationIdentificationInfo = new()
                {
                    LegalPerson = new() { Name = legalPerson.Attribute("НаимОрг").Value, Inn = legalPerson.Attribute("ИННЮЛ").Value, Kpp = legalPerson.Attribute("КПП")?.Value }
                },
                Address = new() { RussianAddress = new() { RegionCode = "77", Region = "г. Москва" } }
            };
        }

        /// <summary>
        /// Идентификатор участника ЭДО из имени файла: оператор, дефис, код участника.
        /// </summary>
        private static EdmParticipant ToEdmParticipant(string edoId)
        {
            var parts = edoId.Split('-', 2);
            return new EdmParticipant(parts[0], parts[1]);
        }

        private const string MarkCodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        /// <summary>
        /// Случайный код маркировки: 01 + GTIN + 21 + серийный номер из 13 символов.
        /// </summary>
        private static string CreateRandomMarkCode()
            => "010465029413145221" + new string(Random.Shared.GetItems(MarkCodeAlphabet.AsSpan(), 13));

        /// <summary>
        /// УПД на стенде: участники ЭДО и стороны берутся из уже отправленного УПД (ИдФайл ON_NSCHFDOPPR_{получатель}_{отправитель}_…), подписант - владелец сертификата.
        /// ИдФайл не задаётся: его, как и в приложении, формирует модель CIS.EDM.
        /// </summary>
        /// <remarks>
        /// Строка с кодом маркировки делает УПД маркированным: после титула покупателя такой документ уходит в ГИС МТ,
        /// статус «Подписан: Отправлен в ГИС МТ». Код каждый раз новый, чтобы УПД на стенде не повторяли его.
        /// </remarks>
        private async Task<VS.SellerUniversalTransferDocument> CreateStandUniversalTransferDocumentAsync(TrueApiCRPTProvider provider, CRPTOption settings)
        {
            var list = await provider.GetOutgoingDocumentListAsync(settings, new DocumentCollectionSearchModel { Limit = 50 });
            var documentId = FindDocumentId(list, "исходящих", IsUpd, "УПД и УПДи");
            var file = XDocument.Parse(await provider.GetOutgoingDocumentAsync(settings, documentId)).Root;

            var fileIdParts = file.Attribute("ИдФайл").Value.Split('_');
            var invoice = file.Element("Документ").Element("СвСчФакт");
            var seller = ReadLegalPerson(invoice, "СвПрод");
            var owner = GetCertificateOwner();
            var now = DateTime.UtcNow;

            return new VS.SellerUniversalTransferDocument
            {
                SenderEdmParticipant = ToEdmParticipant(fileIdParts[3]),
                RecipientEdmParticipant = ToEdmParticipant(fileIdParts[2]),
                Function = UniversalTransferDocumentFunction.СЧФДОП,
                DocumentNumber = $"Т-{now:yyyyMMddHHmmss}",
                DocumentDate = now.Date,
                DocumentCreator = seller,
                CurrencyCode = "643",
                CurrencyName = "Российский рубль",
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
                        TaxRate = TaxRate.TwentyTwoPercent,
                        Vat = 22,
                        Sum = 122,
                        AdditionalInfo = new() { ItemIdentificationNumberList = [new() { MarkItems = [CreateRandomMarkCode()] }] }
                    }
                ],
                TransferInfo = new() { TransferDetails = new() { Date = now.Date } },
                Signers =
                [
                    new()
                    {
                        JobTitle = "Директор",
                        AuthorityConfirmationType = VR.AuthorityConfirmationType.ElectronicSignatureData,
                        Person = new() { Surname = owner.Surname, FirstName = owner.FirstName, Patronymic = owner.Patronymic }
                    }
                ]
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
            var provider = CreateProvider();
            var document = await CreateStandUniversalTransferDocumentAsync(provider, settings);

            // Act
            var result = await provider.PostUniversalTransferDocumentAsync(settings, document, isDraft: true);

            // Assert
            TestContext.Out.WriteLine(result.Exception);
            Assert.That(result.Exception, Is.Null);

            TestContext.Out.WriteLine(result.Id);
            var fileId = XDocument.Parse(result.Content).Root.Attribute("ИдФайл").Value;
            var content = await provider.GetOutgoingDocumentAsync(settings, result.Id);
            Assert.That(AssertFnsXml(content).Attribute("ИдФайл").Value, Is.EqualTo(fileId));
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
            var provider = CreateProvider();

            // Новые черновики первыми: старые на стенде могут быть в версии 5.01.
            var draftSearchModel = new DocumentCollectionSearchModel { Limit = 50, Status = [DocumentState.Draft], SortBy = SortField.created_at };
            var drafts = await provider.GetOutgoingDocumentListAsync(settings, draftSearchModel);
            var documentId = FindDocumentId(drafts, "черновиков исходящих", IsUpd970, "УПД и УПДи формата 970");

            // Act
            await provider.SignOutgoingDocumentAsync(settings, documentId);

            // Assert
            var draftsAfterSigning = await provider.GetOutgoingDocumentListAsync(settings, draftSearchModel);
            Assert.That(draftsAfterSigning.Items.SelectMany(x => x.Documents).Select(x => x.Id), Does.Not.Contain(documentId));
        }

        /// <summary>
        /// Входящие документы, которые ждут подписи: доставлен (3) и просмотрен (13). Два статуса - два запроса, провайдер объединяет ответы сам.
        /// </summary>
        private static Task<DocumentCollection> GetDocumentsAwaitingSignatureAsync(TrueApiCRPTProvider provider, CRPTOption settings)
            => provider.GetIncomingDocumentListAsync(settings, new DocumentCollectionSearchModel
            {
                Limit = 50,
                Status = [DocumentState.DeliveredSignatureRequired, DocumentState.ReviewedSignatureRequired],
                SortBy = SortField.created_at
            });

        /// <summary>
        /// Титул покупателя бывает только у функций ДОП и СЧФДОП.
        /// </summary>
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
            var provider = CreateProvider();

            var awaiting = await GetDocumentsAwaitingSignatureAsync(provider, settings);
            var documentId = FindDocumentId(awaiting, "ждущих подписи входящих", BuyerTitleDocumentTypes.Contains, "УПД и УПДи формата 970 с функцией ДОП или СЧФДОП");

            var file = XDocument.Parse(await provider.GetIncomingDocumentAsync(settings, documentId)).Root;
            var owner = GetCertificateOwner();
            var document = new VB.BuyerUniversalTransferDocument
            {
                EdmDocumentId = documentId,
                DocumentCreator = ReadLegalPerson(file.Element("Документ").Element("СвСчФакт"), "СвПокуп"),
                AcceptanceInfo = new() { OperationName = "Товары приняты без претензий", Date = DateTime.UtcNow.Date },
                Signers =
                [
                    new()
                    {
                        JobTitle = "Директор",
                        AuthorityConfirmationType = VR.AuthorityConfirmationType.ElectronicSignatureData,
                        Person = new() { Surname = owner.Surname, FirstName = owner.FirstName, Patronymic = owner.Patronymic }
                    }
                ]
            };

            // Act
            var result = await provider.ReceiptUniversalTransferDocumentAsync(settings, document);

            // Assert
            TestContext.Out.WriteLine(result.Exception);
            TestContext.Out.WriteLine(result.Id);
            TestContext.Out.WriteLine(result.Content);
            var awaitingAfterTitle = await GetDocumentsAwaitingSignatureAsync(provider, settings);
            Assert.Multiple(() =>
            {
                Assert.That(result.Exception, Is.Null);
                Assert.That(result.Id, Is.Not.Empty);
                Assert.That(awaitingAfterTitle.Items.SelectMany(x => x.Documents).Select(x => x.Id), Does.Not.Contain(documentId));
            });
        }
    }
}
