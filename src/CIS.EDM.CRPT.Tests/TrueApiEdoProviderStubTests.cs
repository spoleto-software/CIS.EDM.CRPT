using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Spoleto.TrueApi.Auth.Models;
using Spoleto.TrueApi.Auth.Models.Options;
using Spoleto.TrueApi.Auth.Providers;
using Spoleto.TrueApi.Edo.Enums;
using Spoleto.TrueApi.Edo.Exceptions;
using Spoleto.TrueApi.Edo.Models;
using Spoleto.TrueApi.Edo.Models.Options;
using Spoleto.TrueApi.Edo.Models.Upd;
using Spoleto.TrueApi.Edo.Providers;

#nullable enable

namespace CIS.EDM.CRPT.Tests
{
    /// <summary>
    /// <see cref="TrueApiEdoProvider"/> на подставном транспорте: адреса, заголовки, повтор после 401, ошибки. Стенд не нужен.
    /// </summary>
    public class TrueApiEdoProviderStubTests
    {
        private const string ServiceUrl = "https://markirovka.sandbox.crptech.ru";

        // Второй пример ответа из описания метода 9.2.8 True API v.746.0, плюс суммы и тип строками, как в таблице параметров.
        private const string DocumentListJson = """
            {
              "items": [
                {
                  "id": "33a333ee-3cb3-3aae-3f33-d33ce333333c",
                  "created_at": 1582097164,
                  "date": 1582059600,
                  "documents": [
                    {
                      "id": "220262cf-0fe7-4d3f-996a-038181ba7812",
                      "created_at": 1776253164,
                      "date": 1775077200,
                      "number": "УПД № группсы от 02.04.2026",
                      "processed_at": 1776253164,
                      "revision": null,
                      "status": 3,
                      "total_price": 100,
                      "total_vat_amount": 0,
                      "type": 520,
                      "is_title_signed": false,
                      "created_by": 0,
                      "created_hub": false,
                      "is_closed_lm_part": null,
                      "package_status": null,
                      "uni_new_msg": null
                    }
                  ],
                  "group_id": "4d44444e-4d44-4444-4e44-a4cd444ed444",
                  "number": "номер документа",
                  "recipient": {
                    "id": 999999999,
                    "extra_id": "00000000-23c4-3685-0000-000000000000",
                    "inn": "0000000000",
                    "kpp": "111111111",
                    "ogrn": "1111111111111",
                    "name": "ООО",
                    "fio": null,
                    "address": null,
                    "legal_address": null,
                    "status": "REGISTERED"
                  },
                  "status": 1,
                  "total_price": "1930500",
                  "total_vat_amount": "279500",
                  "type": "504",
                  "create_time_stamp": 1582059600,
                  "export_time_stamp": 1582097180,
                  "product_group": -1,
                  "active_type": 0
                }
              ],
              "has_next_page": true
            }
            """;

        private const string DocumentId = "220262cf-0fe7-4d3f-996a-038181ba7812";

        // Имя файла продавца формата 970: получатель, отправитель, дата, GUID и признаки N2-N6 (маркированный товар).
        private const string SellerFileName = "ON_NSCHFDOPPR_2BM-0000000000-000000000_2BM-7777777777-777777777_20260402_" + DocumentId + "_0_1_0_0_0_00";

        private const string EventId = "000a0a00-0aaa-0a00-000a-0a00a0a0aa00";

        // Начало подписи CMS в Base64 с переносом строки, как её возвращает КриптоПро.
        private const string SignatureBase64 = "MIIPkQYJKoZIhvcNAQcCoIIPgjCCD34CAQExDjAMBggqhQMHAQECAwUAMAsGCSqG\r\nSIb3DQEHAaCCCRUwggkRMIIIvqADAgECAhEBr1uLAFartopGqtxJllj2fjAKBggq";

        // Архив УПД: информация продавца, информация покупателя и квитанция, у каждого файла подпись.
        private static readonly string[] DocumentArchiveEntries =
        [
            SellerFileName + ".xml",
            SellerFileName + ".p7s",
            "ON_NSCHFDOPPOK_2BM-7777777777-777777777_2BM-0000000000-000000000_20260403_" + DocumentId + ".xml",
            "ON_NSCHFDOPPOK_2BM-7777777777-777777777_2BM-0000000000-000000000_20260403_" + DocumentId + ".p7s",
            "DP_IZVPOL_2BM-7777777777-777777777_2BM-0000000000-000000000_20260402_" + DocumentId + ".xml",
            "DP_IZVPOL_2BM-7777777777-777777777_2BM-0000000000-000000000_20260402_" + DocumentId + ".p7s"
        ];

        static TrueApiEdoProviderStubTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//support for "windows-1251"
        }

        private static TrueApiEdoProviderOption CreateSettings() => new()
        {
            ServiceUrl = ServiceUrl,
            CertificateThumbprint = "thumbprint",
            Inn = "7777777777"
        };

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content, string mediaType)
            => new(statusCode) { Content = new StringContent(content, Encoding.UTF8, mediaType) };

        /// <summary>
        /// Файл .xml в архиве - XML в windows-1251 с ИдФайл по своему имени, остальные файлы - байты своего имени. Так по содержимому видно, какой файл вернулся.
        /// </summary>
        private static HttpResponseMessage CreateArchiveResponse(string? archiveFileName, params string[] entryNames)
            => CreateArchiveResponse(archiveFileName, entryNames.ToDictionary(x => x, CreateEntryContent));

        private static byte[] CreateEntryContent(string entryName)
            => entryName.EndsWith(".xml", StringComparison.Ordinal)
                ? Encoding.GetEncoding("windows-1251").GetBytes(CreateEntryXml(Path.GetFileNameWithoutExtension(entryName)))
                : Encoding.UTF8.GetBytes(entryName);

        private static string CreateEntryXml(string fileName) => $"""<?xml version="1.0" encoding="windows-1251"?><Файл ИдФайл="{fileName}"/>""";

        private static HttpResponseMessage CreateArchiveResponse(string? archiveFileName, IReadOnlyDictionary<string, byte[]> entries)
        {
            using var archiveStream = new MemoryStream();
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
            {
                foreach (var (entryName, content) in entries)
                {
                    using var entryStream = archive.CreateEntry(entryName).Open();
                    entryStream.Write(content);
                }
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(archiveStream.ToArray()) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            if (archiveFileName != null)
            {
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = $"\"{archiveFileName}\"" };
            }

            return response;
        }

        /// <summary>
        /// Информация продавца УПД формата 970: только элементы и атрибуты, которые повторяет информация покупателя.
        /// </summary>
        private static string CreateSellerXml(string? correctionNumber)
        {
            var correction = correctionNumber == null ? "" : $"""<ИспрДок НомИспр="{correctionNumber}" ДатаИспр="03.04.2026"/>""";

            return $"""
                <?xml version="1.0" encoding="windows-1251"?>
                <Файл ИдФайл="{SellerFileName}" ВерсФорм="5.03" ВерсПрог="Spoleto">
                  <Документ КНД="1115131" Функция="СЧФДОП" НаимДокОпр="Счет-фактура и документ об отгрузке товаров (выполнении работ), передаче имущественных прав (документ об оказании услуг)" ДатаИнфПр="02.04.2026" ВремИнфПр="10.15.00" УИД="0000000000-7777777777-000001">
                    <СвСчФакт НомерДок="15" ДатаДок="01.04.2026" КодОКВ="643">{correction}</СвСчФакт>
                  </Документ>
                </Файл>
                """;
        }

        private static HttpResponseMessage CreateSellerArchiveResponse(string sellerXml, byte[] signature)
            => CreateArchiveResponse(SellerFileName + ".zip", new Dictionary<string, byte[]>
            {
                [SellerFileName + ".xml"] = Encoding.GetEncoding("windows-1251").GetBytes(sellerXml),
                [SellerFileName + ".p7s"] = signature
            });

        private static readonly string[] MarkCodes = ["010460000000000121abc", "010460000000000121def"];

        /// <summary>
        /// УПД СЧФДОП из одной строки 22%: маркированный товар с двумя кодами или обычный.
        /// </summary>
        private static EdoSellerUniversalTransferDocument CreateUniversalTransferDocument(bool isMarked = true, string? revisionNumber = null)
        {
            var seller = new EdoOrganization
            {
                OrganizationIdentificationInfo = new() { LegalPerson = new() { Name = "ООО «Продавец»", Inn = "7777777777", Kpp = "777701001" } },
                Address = new() { RussianAddress = new() { RegionCode = "77", Region = "г. Москва" } }
            };

            return new EdoSellerUniversalTransferDocument
            {
                SenderEdoId = "2LT-2",
                RecipientEdoId = "2LT-1",
                DateCreation = new DateTime(2026, 4, 2, 10, 15, 0, DateTimeKind.Utc),
                Function = EdoDocumentFunction.InvoiceAndPrimaryDocument,
                DocumentNumber = "15",
                DocumentDate = new DateTime(2026, 4, 1),
                RevisionNumber = revisionNumber,
                RevisionDate = revisionNumber == null ? null : new DateTime(2026, 4, 3),
                DocumentCreator = seller,
                Sellers = [seller],
                Buyers = [new() { OrganizationIdentificationInfo = new() { LegalPerson = new() { Name = "ООО «Покупатель»", Inn = "0000000000", Kpp = "000001001" } } }],
                Items =
                [
                    new()
                    {
                        ProductName = "Кроссовки",
                        UnitCode = "796",
                        UnitName = "шт",
                        Quantity = 2,
                        Price = 100,
                        SumWithoutVat = 200,
                        TaxRate = EdoTaxRate.TwentyTwoPercent,
                        Vat = 44,
                        Sum = 244,
                        AdditionalInfo = isMarked ? new() { ItemIdentificationNumberList = [new() { MarkItems = [.. MarkCodes] }] } : null
                    }
                ],
                TransferInfo = new() { TransferDetails = new() { Date = new DateTime(2026, 4, 2) } },
                Signers =
                [
                    new()
                    {
                        JobTitle = "Директор",
                        AuthorityConfirmationType = EdoAuthorityConfirmationType.ElectronicSignatureData,
                        Person = new() { Surname = "Иванов", FirstName = "Иван", Patronymic = "Иванович" }
                    }
                ]
            };
        }

        /// <summary>
        /// Информация покупателя к <see cref="DocumentId"/>: приняли с расхождениями по акту, товар получил кладовщик.
        /// </summary>
        private static EdoBuyerUniversalTransferDocument CreateBuyerUniversalTransferDocument() => new()
        {
            DocumentId = DocumentId,
            DateCreation = new DateTime(2026, 4, 3, 9, 30, 0, DateTimeKind.Utc),
            DocumentCreator = new() { OrganizationIdentificationInfo = new() { LegalPerson = new() { Name = "ООО «Покупатель»", Inn = "0000000000", Kpp = "000001001" } } },
            OperationType = "Поставка",
            AcceptanceInfo = new()
            {
                OperationName = "Товары приняты с расхождениями",
                Date = new DateTime(2026, 4, 3),
                OperationNameInfo = new()
                {
                    Code = EdoOperationNameCode.AcceptedWithDiscrepancies,
                    DiscrepancyDocument = new() { Name = "Акт о расхождениях", Number = "Р-1", Date = new DateTime(2026, 4, 3) }
                },
                ReceiverPerson = new() { Employee = new() { Surname = "Сидоров", FirstName = "Сидор", JobTitle = "Кладовщик" } }
            },
            Signers =
            [
                new()
                {
                    JobTitle = "Директор",
                    AuthorityConfirmationType = EdoAuthorityConfirmationType.ElectronicSignatureData,
                    Person = new() { Surname = "Петров", FirstName = "Петр", Patronymic = "Петрович" }
                }
            ]
        };

        /// <summary>
        /// Файл черновика как есть. В тестах подписания - XML в windows-1251 с кириллицей: перекодирование изменило бы его байты.
        /// </summary>
        private static HttpResponseMessage CreateDraftFileResponse(byte[] content)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return response;
        }

        [Test]
        public async Task GetIncomingDocumentList_BuildsQueryAndParsesResponse()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, DocumentListJson, "application/json"));
            var tokenProvider = new FakeTokenProvider();
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, tokenProvider);
            var searchModel = new EdoDocumentSearchModel
            {
                Limit = 10,
                Offset = 20,
                CreatedFrom = DateTimeOffset.FromUnixTimeSeconds(1582059600).UtcDateTime,
                PartnerInn = "0000000000",
                Status = EdoDocumentStatus.DeliveredSignatureRequired,
                Type = EdoDocumentType.UPD970WithSCHFDOPInvoiceWaybill,
                Folder = EdoFolder.Documents,
                SortBy = EdoDocumentSortField.CreatedAt,
                Asc = true
            };

            // Act
            var data = await edoProvider.GetIncomingDocumentListAsync(CreateSettings(), searchModel);

            // Assert
            var request = handler.Requests.Single();
            Assert.Multiple(() =>
            {
                Assert.That(request.Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/elk/incoming-documents"
                    + "?limit=10&offset=20&created_from=1582059600&partner_inn=0000000000&status=3&type=524&folder=0&sortBy=created_at&asc=true"));
                Assert.That(request.Authorization, Is.EqualTo("Bearer uuid-1"));
                Assert.That(request.Accept, Is.EqualTo("application/json"));
                Assert.That(tokenProvider.LastOption, Is.EqualTo(new TrueApiTokenProviderOption(ServiceUrl, "thumbprint", "7777777777", true)));

                Assert.That(data.HasNextPage, Is.True);
                var group = data.Items.Single();
                Assert.That(group.Status, Is.EqualTo(EdoDocumentStatus.Sent));
                Assert.That(group.Type, Is.EqualTo(EdoDocumentType.UPDWithSCHFDOPInvoiceWaybill));
                Assert.That(group.TotalPrice, Is.EqualTo(1930500m));
                Assert.That(group.CreatedAt, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1582097164).LocalDateTime));
                Assert.That(group.CreatedAt!.Value.Kind, Is.EqualTo(DateTimeKind.Local), "Как в CIS.EDM: даты из ответа - местное время.");
                Assert.That(group.Recipient!.Id, Is.EqualTo(999999999));
                Assert.That(group.Recipient.Inn, Is.EqualTo("0000000000"));

                var document = group.Documents.Single();
                Assert.That(document.Id, Is.EqualTo("220262cf-0fe7-4d3f-996a-038181ba7812"));
                Assert.That(document.Type, Is.EqualTo(EdoDocumentType.UPD970WithDOPWaybill));
                Assert.That(document.Status, Is.EqualTo(EdoDocumentStatus.DeliveredSignatureRequired));
                Assert.That(document.Revision, Is.Null);
                Assert.That(document.IsTitleSigned, Is.False);
            });
        }

        [TestCase(DateTimeKind.Unspecified)]
        [TestCase(DateTimeKind.Local)]
        public async Task GetDocumentList_CreatedFromNotUtc_IsLocalTime(DateTimeKind kind)
        {
            // Arrange
            var createdFrom = new DateTime(2026, 10, 30, 0, 0, 0, kind);
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, """{"items":[]}""", "application/json"));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            await edoProvider.GetIncomingDocumentListAsync(CreateSettings(), new EdoDocumentSearchModel { CreatedFrom = createdFrom });

            // Assert
            // Как в CIS.EDM: граница без Kind и местная - местное время.
            var expected = new DateTimeOffset(createdFrom, TimeZoneInfo.Local.GetUtcOffset(createdFrom)).ToUnixTimeSeconds();
            Assert.That(handler.Requests.Single().Uri.Query, Is.EqualTo($"?created_from={expected}"));
        }

        [TestCase(false, "incoming-documents")]
        [TestCase(true, "outgoing-documents")]
        public async Task GetDocumentList_WithoutSearchModel_HasNoQuery(bool isOutgoing, string documentsPath)
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, """{"items":[],"has_next_page":false}""", "application/json"));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var data = isOutgoing
                ? await edoProvider.GetOutgoingDocumentListAsync(CreateSettings())
                : await edoProvider.GetIncomingDocumentListAsync(CreateSettings());

            // Assert
            Assert.That(handler.Requests.Single().Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/elk/" + documentsPath));
            Assert.That(data.Items, Is.Empty);
        }

        [TestCase(false, "incoming-documents")]
        [TestCase(true, "outgoing-documents")]
        public async Task GetDocument_ReturnsFileBytesAsIs(bool isOutgoing, string documentsPath)
        {
            // Arrange
            var xml = Encoding.GetEncoding("windows-1251").GetBytes("<?xml version=\"1.0\" encoding=\"windows-1251\"?><Файл ИдФайл=\"ON_NSCHFDOPPR\"/>");
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(xml) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var handler = new StubHttpMessageHandler(response);
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var content = isOutgoing
                ? await edoProvider.GetOutgoingDocumentAsync(CreateSettings(), "f2aab44e-9cc3-4b37-a4ff-c8edf0b6e57b")
                : await edoProvider.GetIncomingDocumentAsync(CreateSettings(), "f2aab44e-9cc3-4b37-a4ff-c8edf0b6e57b");

            // Assert
            var request = handler.Requests.Single();
            Assert.Multiple(() =>
            {
                Assert.That(request.Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/elk/" + documentsPath + "/f2aab44e-9cc3-4b37-a4ff-c8edf0b6e57b/content"));
                Assert.That(request.Accept, Is.EqualTo("*/*"));
                Assert.That(content, Is.EqualTo(xml));
                Assert.That(XDocument.Load(new MemoryStream(content)).Root!.Attribute("ИдФайл")!.Value, Is.EqualTo("ON_NSCHFDOPPR"));
            });
        }

        [TestCase(false, "incoming-documents")]
        [TestCase(true, "outgoing-documents")]
        public async Task GetSignedDocumentInfo_ReadsFileNamedAfterArchive(bool isOutgoing, string documentsPath)
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateArchiveResponse(SellerFileName + ".zip", DocumentArchiveEntries));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var document = isOutgoing
                ? await edoProvider.GetOutgoingSignedDocumentInfoAsync(CreateSettings(), DocumentId)
                : await edoProvider.GetIncomingSignedDocumentInfoAsync(CreateSettings(), DocumentId);

            // Assert
            var request = handler.Requests.Single();
            Assert.Multiple(() =>
            {
                Assert.That(request.Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/elk/" + documentsPath + "/" + DocumentId));
                Assert.That(request.Accept, Is.EqualTo("*/*"));
                Assert.That(document.FileName, Is.EqualTo(SellerFileName));
                Assert.That(document.Content, Is.EqualTo(CreateEntryXml(SellerFileName)));
                Assert.That(document.RawContent, Is.EqualTo(CreateEntryContent(SellerFileName + ".xml")));
                Assert.That(Encoding.UTF8.GetString(document.Signature), Is.EqualTo(SellerFileName + ".p7s"));
            });
        }

        [TestCase(null)]
        [TestCase(DocumentId + ".zip")]
        public async Task GetSignedDocumentInfo_ArchiveNotNamedAfterDocument_ReadsSellerFile(string? archiveFileName)
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateArchiveResponse(archiveFileName, DocumentArchiveEntries));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var document = await edoProvider.GetIncomingSignedDocumentInfoAsync(CreateSettings(), DocumentId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(document.FileName, Is.EqualTo(SellerFileName));
                Assert.That(document.Content, Is.EqualTo(CreateEntryXml(SellerFileName)));
                Assert.That(document.RawContent, Is.EqualTo(CreateEntryContent(SellerFileName + ".xml")));
                Assert.That(Encoding.UTF8.GetString(document.Signature), Is.EqualTo(SellerFileName + ".p7s"));
            });
        }

        [TestCase("windows-1251", false)]
        [TestCase("utf-8", false)]
        [TestCase("utf-8", true)]
        public async Task GetSignedDocumentInfo_DecodesXmlByDeclaredEncoding(string encodingName, bool withBom)
        {
            // Arrange
            var encoding = Encoding.GetEncoding(encodingName);
            var xml = $"""<?xml version="1.0" encoding="{encodingName}"?><Файл ИдФайл="{SellerFileName}"/>""";
            byte[] rawContent = withBom ? [.. encoding.GetPreamble(), .. encoding.GetBytes(xml)] : encoding.GetBytes(xml);
            var handler = new StubHttpMessageHandler(CreateArchiveResponse(SellerFileName + ".zip", new Dictionary<string, byte[]>
            {
                [SellerFileName + ".xml"] = rawContent,
                [SellerFileName + ".p7s"] = [0x30]
            }));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var document = await edoProvider.GetIncomingSignedDocumentInfoAsync(CreateSettings(), DocumentId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(document.Content, Is.EqualTo(xml));
                Assert.That(document.RawContent, Is.EqualTo(rawContent));
            });
        }

        [Test]
        public void GetSignedDocumentInfo_WithoutSellerFile_ThrowsWithArchiveContents()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateArchiveResponse(null, DocumentArchiveEntries.Skip(2).ToArray()));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var ex = Assert.ThrowsAsync<InvalidDataException>(() => edoProvider.GetIncomingSignedDocumentInfoAsync(CreateSettings(), DocumentId));

            // Assert
            Assert.That(ex!.Message, Does.Contain(DocumentArchiveEntries[2]));
        }

        [Test]
        public void GetSignedDocumentInfo_WithoutSignature_Throws()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateArchiveResponse(SellerFileName + ".zip", SellerFileName + ".xml"));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var ex = Assert.ThrowsAsync<InvalidDataException>(() => edoProvider.GetOutgoingSignedDocumentInfoAsync(CreateSettings(), DocumentId));

            // Assert
            Assert.That(ex!.Message, Does.Contain(SellerFileName + ".p7s"));
        }

        [TestCase(false, "incoming-documents", null, false)]
        [TestCase(true, "outgoing-documents", "1", true)]
        public async Task GetSellerDocumentInfo_ParsesSellerFile(bool isOutgoing, string documentsPath, string? correctionNumber, bool isUPDi)
        {
            // Arrange
            var signature = new byte[] { 0x30, 0x82, 0x01, 0x00 };
            var handler = new StubHttpMessageHandler(CreateSellerArchiveResponse(CreateSellerXml(correctionNumber), signature));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var info = isOutgoing
                ? await edoProvider.GetOutgoingSellerDocumentInfoAsync(CreateSettings(), DocumentId)
                : await edoProvider.GetIncomingSellerDocumentInfoAsync(CreateSettings(), DocumentId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(handler.Requests.Single().Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/elk/" + documentsPath + "/" + DocumentId));
                Assert.That(info.FileId, Is.EqualTo(SellerFileName));
                Assert.That(info.DateCreation, Is.EqualTo("02.04.2026"));
                Assert.That(info.TimeCreation, Is.EqualTo("10.15.00"));
                Assert.That(info.DocumentName, Does.StartWith("Счет-фактура и документ об отгрузке"));
                Assert.That(info.Function, Is.EqualTo(EdoDocumentFunction.InvoiceAndPrimaryDocument));
                Assert.That(info.DocumentNumber, Is.EqualTo("15"));
                Assert.That(info.DocumentDate, Is.EqualTo("01.04.2026"));
                Assert.That(info.DocumentUid, Is.EqualTo("0000000000-7777777777-000001"));
                Assert.That(info.IsUPDi, Is.EqualTo(isUPDi));
                Assert.That(info.Signature, Is.EqualTo(signature));
            });
        }

        [TestCase("Функция=\"СЧФДОП\"", "Функция=\"КСЧФ\"", "КСЧФ")]
        [TestCase(" ДатаДок=\"01.04.2026\"", "", "ДатаДок")]
        [TestCase("СвСчФакт", "СвКСчФ", "СвСчФакт")]
        public void GetSellerDocumentInfo_InvalidSellerFile_ThrowsWithReason(string oldValue, string newValue, string expectedReason)
        {
            // Arrange
            var sellerXml = CreateSellerXml(null).Replace(oldValue, newValue);
            var handler = new StubHttpMessageHandler(CreateSellerArchiveResponse(sellerXml, [0x30]));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var ex = Assert.ThrowsAsync<InvalidDataException>(() => edoProvider.GetIncomingSellerDocumentInfoAsync(CreateSettings(), DocumentId));

            // Assert
            Assert.That(ex!.Message, Does.Contain(expectedReason).And.Contain(SellerFileName));
        }

        [Test]
        public async Task GetSellerDocumentInfo_InvoiceWithoutDocumentName_ReadsWithoutName()
        {
            // Arrange
            // У счета-фактуры без документа об отгрузке нет НаимДокОпр.
            var sellerXml = Regex.Replace(CreateSellerXml(null).Replace("Функция=\"СЧФДОП\"", "Функция=\"СЧФ\""), " НаимДокОпр=\"[^\"]+\"", "");
            var handler = new StubHttpMessageHandler(CreateSellerArchiveResponse(sellerXml, [0x30]));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var info = await edoProvider.GetIncomingSellerDocumentInfoAsync(CreateSettings(), DocumentId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(info.Function, Is.EqualTo(EdoDocumentFunction.Invoice));
                Assert.That(info.DocumentName, Is.Null);
                Assert.That(info.FileId, Is.EqualTo(SellerFileName));
            });
        }

        [Test]
        public async Task SignOutgoingDocument_SignsFileBytesAndPostsSignature()
        {
            // Arrange
            var content = Encoding.GetEncoding("windows-1251").GetBytes(CreateSellerXml(null));
            var handler = new StubHttpMessageHandler(CreateDraftFileResponse(content), new HttpResponseMessage(HttpStatusCode.OK));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            await edoProvider.SignOutgoingDocumentAsync(CreateSettings(), DocumentId);

            // Assert
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
            var fileRequest = handler.Requests[0];
            var signatureRequest = handler.Requests[1];
            Assert.Multiple(() =>
            {
                Assert.That(fileRequest.Method, Is.EqualTo(HttpMethod.Get));
                Assert.That(fileRequest.Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/elk/outgoing-documents/" + DocumentId + "/content"));
                Assert.That(edoProvider.SignedContents.Single(), Is.EqualTo(content));
                Assert.That(edoProvider.Thumbprint, Is.EqualTo("thumbprint"));

                Assert.That(signatureRequest.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(signatureRequest.Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/edo/outgoing-documents/" + DocumentId + "/signature"));
                Assert.That(signatureRequest.ContentType, Is.EqualTo("text/plain"));
                Assert.That(signatureRequest.ContentEncoding, Is.EqualTo("base64"));
                Assert.That(signatureRequest.Body, Is.EqualTo(SignatureBase64));
            });
        }

        [Test]
        public async Task SignOutgoingDocument_Unauthorized_ResendsSameSignature()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(
                CreateDraftFileResponse(Encoding.GetEncoding("windows-1251").GetBytes(CreateSellerXml(null))),
                CreateResponse(HttpStatusCode.Unauthorized, """{"error_message":"Токен не действителен. Необходимо получить новый токен аутентификации"}""", "application/json"),
                new HttpResponseMessage(HttpStatusCode.OK));
            var tokenProvider = new FakeTokenProvider();
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), tokenProvider, SignatureBase64);

            // Act
            await edoProvider.SignOutgoingDocumentAsync(CreateSettings(), DocumentId);

            // Assert
            // Подставной провайдер выдаёт новый токен на каждый запрос: uuid-1 ушёл с запросом файла.
            var signatureRequests = handler.Requests.Skip(1).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(edoProvider.SignedContents, Has.Count.EqualTo(1));
                Assert.That(tokenProvider.ExpiredCount, Is.EqualTo(1));
                Assert.That(signatureRequests.Select(x => x.Authorization), Is.EqualTo(new[] { "Bearer uuid-2", "Bearer uuid-3" }));
                Assert.That(signatureRequests.Select(x => x.Body), Is.EqualTo(new[] { SignatureBase64, SignatureBase64 }));
                Assert.That(signatureRequests.Select(x => x.ContentEncoding), Is.EqualTo(new[] { "base64", "base64" }));
            });
        }

        [Test]
        public void SignOutgoingDocument_DraftNotFound_DoesNotSign()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.NotFound, """{"error_message":"Документ не найден в ГИС МТ"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => edoProvider.SignOutgoingDocumentAsync(CreateSettings(), DocumentId));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Is.EqualTo("Документ не найден в ГИС МТ"));
                Assert.That(edoProvider.SignedContents, Is.Empty);
                Assert.That(handler.Requests, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task PostUniversalTransferDocument_Draft_PostsFileWithoutSignature()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, $$"""{"id":"{{DocumentId}}"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var result = await edoProvider.PostUniversalTransferDocumentAsync(CreateSettings(), CreateUniversalTransferDocument(), isDraft: true);

            // Assert
            var request = handler.Requests.Single();
            var part = request.Parts!.Single();
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(DocumentId));
                Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(request.Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/edo/outgoing-documents"));
                Assert.That(request.ContentType, Is.EqualTo("multipart/form-data"));
                Assert.That(part.Name, Is.EqualTo("content"));
                Assert.That(part.FileName, Is.EqualTo(result.FileId + ".xml"));
                Assert.That(part.ContentType, Is.EqualTo("text/xml"));
                Assert.That(part.Content, Is.EqualTo(result.RawContent));
                Assert.That(result.RawContent, Is.EqualTo(Encoding.GetEncoding("windows-1251").GetBytes(result.Content)));
                Assert.That(XDocument.Load(new MemoryStream(result.RawContent)).Root!.Attribute("ИдФайл")!.Value, Is.EqualTo(result.FileId));
                Assert.That(edoProvider.SignedContents, Is.Empty);
            });
        }

        [Test]
        public async Task PostUniversalTransferDocument_SignsFileBytesAndPostsSignature()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, $$"""{"id":"{{DocumentId}}"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var result = await edoProvider.PostUniversalTransferDocumentAsync(CreateSettings(), CreateUniversalTransferDocument());

            // Assert
            var parts = handler.Requests.Single().Parts!;
            Assert.That(parts.Select(x => x.Name), Is.EqualTo(new[] { "content", "signature" }));
            Assert.Multiple(() =>
            {
                Assert.That(edoProvider.SignedContents.Single(), Is.EqualTo(result.RawContent));
                Assert.That(parts[0].Content, Is.EqualTo(result.RawContent));
                Assert.That(parts[1].ContentType, Is.EqualTo("text/plain"));
                Assert.That(parts[1].ContentEncoding, Is.EqualTo("base64"));
                Assert.That(Encoding.UTF8.GetString(parts[1].Content), Is.EqualTo(SignatureBase64));
            });
        }

        [Test]
        public async Task PostUniversalTransferDocument_Updi_PostsToUpdiMethod()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, $$"""{"id":"{{DocumentId}}"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var result = await edoProvider.PostUniversalTransferDocumentAsync(CreateSettings(), CreateUniversalTransferDocument(revisionNumber: "1"), isDraft: true);

            // Assert
            var revision = XDocument.Parse(result.Content).Root!.Element("Документ")!.Element("СвСчФакт")!.Element("ИспрДок")!;
            Assert.Multiple(() =>
            {
                Assert.That(handler.Requests.Single().Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/edo/outgoing-documents/xml/updi"));
                Assert.That(revision.Attribute("НомИспр")!.Value, Is.EqualTo("1"));
                Assert.That(revision.Attribute("ДатаИспр")!.Value, Is.EqualTo("03.04.2026"));
            });
        }

        /// <summary>
        /// XML сверен со старым генератором CIS.EDM.CRPT (XmlHelperV5_03) символ в символ, здесь - ключевые места формата 970.
        /// </summary>
        [Test]
        public async Task PostUniversalTransferDocument_CreatesFormat970Xml()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, $$"""{"id":"{{DocumentId}}"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var result = await edoProvider.PostUniversalTransferDocumentAsync(CreateSettings(), CreateUniversalTransferDocument(), isDraft: true);

            // Assert
            var file = XDocument.Parse(result.Content).Root!;
            var document = file.Element("Документ")!;
            var invoice = document.Element("СвСчФакт")!;
            var item = document.Element("ТаблСчФакт")!.Element("СведТов")!;
            var total = document.Element("ТаблСчФакт")!.Element("ВсегоОпл")!;
            Assert.Multiple(() =>
            {
                Assert.That(result.Content, Does.StartWith("""<?xml version="1.0" encoding="windows-1251"?>"""));
                Assert.That(file.Attribute("ВерсФорм")!.Value, Is.EqualTo("5.03"));
                Assert.That(document.Attribute("КНД")!.Value, Is.EqualTo("1115131"));
                Assert.That(document.Attribute("Функция")!.Value, Is.EqualTo("СЧФДОП"));
                Assert.That(document.Attribute("ДатаИнфПр")!.Value, Is.EqualTo("02.04.2026"));
                Assert.That(document.Attribute("ВремИнфПр")!.Value, Is.EqualTo("10.15.00"));
                Assert.That(document.Attribute("НаимЭконСубСост")!.Value, Is.EqualTo("ООО «Продавец», ИНН/КПП: 7777777777/777701001"));
                Assert.That(invoice.Attribute("НомерДок")!.Value, Is.EqualTo("15"));
                Assert.That(invoice.Attribute("ДатаДок")!.Value, Is.EqualTo("01.04.2026"));
                Assert.That(invoice.Element("СвПрод")!.Element("ИдСв")!.Element("СвЮЛУч")!.Attribute("ИННЮЛ")!.Value, Is.EqualTo("7777777777"));
                Assert.That(invoice.Element("СвПокуп")!.Element("ИдСв")!.Element("СвЮЛУч")!.Attribute("ИННЮЛ")!.Value, Is.EqualTo("0000000000"));
                Assert.That(invoice.Element("ДенИзм")!.Attribute("КодОКВ")!.Value, Is.EqualTo("643"));
                Assert.That(item.Attribute("НомСтр")!.Value, Is.EqualTo("1"));
                Assert.That(item.Attribute("НалСт")!.Value, Is.EqualTo("22%"));
                Assert.That(item.Attribute("СтТовУчНал")!.Value, Is.EqualTo("244.00"));
                Assert.That(item.Element("СумНал")!.Element("СумНал")!.Value, Is.EqualTo("44.00"));
                Assert.That(item.Element("ДопСведТов")!.Element("НомСредИдентТов")!.Elements("КИЗ").Select(x => x.Value), Is.EqualTo(MarkCodes));
                Assert.That(total.Attribute("СтТовУчНалВсего")!.Value, Is.EqualTo("244.00"));
                Assert.That(document.Element("СвПродПер")!.Element("СвПер")!.Element("БезДокОснПер")!.Value, Is.EqualTo("1"));
                Assert.That(document.Element("Подписант")!.Attribute("СпосПодтПолном")!.Value, Is.EqualTo("1"));
            });
        }

        [TestCase(true, null, "_0_1_0_0_0_00")]
        [TestCase(false, null, "_0_0_0_0_0_00")]
        [TestCase(false, new[] { EdoGoodsCategory.Alcohol, EdoGoodsCategory.Tobacco }, "_0_0_1_1_0_00")]
        public async Task PostUniversalTransferDocument_GeneratesFileIdFromParticipantsAndCategories(bool isMarked, EdoGoodsCategory[]? categories, string expectedFlags)
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, $$"""{"id":"{{DocumentId}}"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);
            var document = CreateUniversalTransferDocument(isMarked) with { GoodsCategories = categories?.ToList() };

            // Act
            var result = await edoProvider.PostUniversalTransferDocumentAsync(CreateSettings(), document, isDraft: true);

            // Assert
            Assert.That(result.FileId, Does.Match(@"^ON_NSCHFDOPPR_2LT-1_2LT-2_20260402_[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}" + expectedFlags + "$"));
        }

        [Test]
        public void PostUniversalTransferDocument_ItemWithoutVatAmount_ThrowsBeforeRequest()
        {
            // Arrange
            var handler = new StubHttpMessageHandler();
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);
            var document = CreateUniversalTransferDocument();
            document.Items[0].Vat = null;

            // Act
            var ex = Assert.ThrowsAsync<ArgumentException>(() => edoProvider.PostUniversalTransferDocumentAsync(CreateSettings(), document));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Does.Contain("строки 1"));
                Assert.That(handler.Requests, Is.Empty);
                Assert.That(edoProvider.SignedContents, Is.Empty);
            });
        }

        /// <summary>
        /// Файл, который не принял сервис, - в исключении: вызывающий код пишет его в журнал.
        /// </summary>
        [Test]
        public void PostUniversalTransferDocument_ServiceError_ThrowsWithPostedFile()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.BadRequest, """{"error_message":"Файл не прошёл форматный контроль"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var ex = Assert.ThrowsAsync<EdoPostDocumentException>(() => edoProvider.PostUniversalTransferDocumentAsync(CreateSettings(), CreateUniversalTransferDocument()));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Is.EqualTo("Файл не прошёл форматный контроль"));
                Assert.That(ex.InnerException, Is.TypeOf<HttpRequestException>());
                Assert.That(((HttpRequestException)ex.InnerException!).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(ex.RawContent, Is.EqualTo(handler.Requests.Single().Parts![0].Content));
                Assert.That(ex.RawContent, Is.EqualTo(Encoding.GetEncoding("windows-1251").GetBytes(ex.Content)));
                Assert.That(XDocument.Parse(ex.Content).Root!.Attribute("ИдФайл")!.Value, Is.EqualTo(ex.FileId));
            });
        }

        [Test]
        public void PostUniversalTransferDocument_ResponseWithoutId_ThrowsWithPostedFile()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.OK, "{}", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var ex = Assert.ThrowsAsync<EdoPostDocumentException>(() => edoProvider.PostUniversalTransferDocumentAsync(CreateSettings(), CreateUniversalTransferDocument(), isDraft: true));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex!.InnerException, Is.TypeOf<InvalidDataException>());
                Assert.That(ex.Message, Does.Contain(ex.FileId));
                Assert.That(ex.Content, Is.Not.Empty);
            });
        }

        // Описание метода 9.2.9 обещает event_id, а стенд возвращает id.
        [TestCase($$"""{"id":"{{EventId}}"}""")]
        [TestCase($$"""{"event_id":"{{EventId}}"}""")]
        public async Task ReceiptUniversalTransferDocument_SignsTitleAndPostsItWithSellerDocumentId(string responseJson)
        {
            // Arrange
            var handler = new StubHttpMessageHandler(
                CreateSellerArchiveResponse(CreateSellerXml(null), [0x30, 0x82, 0x01, 0x00]),
                CreateResponse(HttpStatusCode.Created, responseJson, "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var result = await edoProvider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), CreateBuyerUniversalTransferDocument());

            // Assert
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
            var sellerRequest = handler.Requests[0];
            var titleRequest = handler.Requests[1];
            var parts = titleRequest.Parts!;
            Assert.That(parts.Select(x => x.Name), Is.EqualTo(new[] { "content", "doc_id", "signature" }));
            Assert.Multiple(() =>
            {
                Assert.That(sellerRequest.Method, Is.EqualTo(HttpMethod.Get));
                Assert.That(sellerRequest.Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/elk/incoming-documents/" + DocumentId));

                Assert.That(result.Id, Is.EqualTo(EventId));
                // Отправитель и получатель файла продавца меняются местами, признаки N2-N6 повторяются.
                Assert.That(result.FileId, Does.Match(@"^ON_NSCHFDOPPOK_2BM-7777777777-777777777_2BM-0000000000-000000000_20260403_[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}_0_1_0_0_0_00$"));
                Assert.That(result.RawContent, Is.EqualTo(Encoding.GetEncoding("windows-1251").GetBytes(result.Content)));
                Assert.That(edoProvider.SignedContents.Single(), Is.EqualTo(result.RawContent));

                Assert.That(titleRequest.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(titleRequest.Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/edo/incoming-documents/xml/upd/title/970"));
                Assert.That(titleRequest.ContentType, Is.EqualTo("multipart/form-data"));
                Assert.That(parts[0].FileName, Is.EqualTo(result.FileId + ".xml"));
                Assert.That(parts[0].ContentType, Is.EqualTo("text/xml"));
                Assert.That(parts[0].Content, Is.EqualTo(result.RawContent));
                Assert.That(Encoding.UTF8.GetString(parts[1].Content), Is.EqualTo(DocumentId));
                Assert.That(parts[2].ContentEncoding, Is.EqualTo("base64"));
                Assert.That(Encoding.UTF8.GetString(parts[2].Content), Is.EqualTo(SignatureBase64));
            });
        }

        [Test]
        public async Task ReceiptUniversalTransferDocument_SellerUpdi_PostsToUpdiTitleMethod()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(
                CreateSellerArchiveResponse(CreateSellerXml("1"), [0x30]),
                CreateResponse(HttpStatusCode.Created, $$"""{"id":"{{EventId}}"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            await edoProvider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), CreateBuyerUniversalTransferDocument());

            // Assert
            Assert.That(handler.Requests[1].Uri.ToString(), Is.EqualTo(ServiceUrl + "/api/v3/true-api/edo/incoming-documents/xml/updi/title/970"));
        }

        /// <summary>
        /// XML сверен со старым генератором CIS.EDM.CRPT (XmlHelperV5_03.Buyer), здесь - ключевые места формата 970
        /// и два места, где старый генератор расходился с форматом: ВидОпер и РеквДокРасх внутри КодСодОпер.
        /// </summary>
        [Test]
        public async Task ReceiptUniversalTransferDocument_CreatesFormat970XmlFromSellerFile()
        {
            // Arrange
            var signature = new byte[] { 0x30, 0x82, 0x01, 0x00 };
            var handler = new StubHttpMessageHandler(
                CreateSellerArchiveResponse(CreateSellerXml(null), signature),
                CreateResponse(HttpStatusCode.Created, $$"""{"id":"{{EventId}}"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var result = await edoProvider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), CreateBuyerUniversalTransferDocument());

            // Assert
            var file = XDocument.Parse(result.Content).Root!;
            var document = file.Element("Документ")!;
            var sellerFile = document.Element("ИдИнфПрод")!;
            var content = document.Element("СодФХЖ4")!;
            var acceptance = content.Element("СвПрин")!;
            Assert.Multiple(() =>
            {
                Assert.That(result.Content, Does.StartWith("""<?xml version="1.0" encoding="windows-1251"?>"""));
                Assert.That(file.Attribute("ИдФайл")!.Value, Is.EqualTo(result.FileId));
                Assert.That(file.Attribute("ВерсФорм")!.Value, Is.EqualTo("5.03"));
                Assert.That(document.Attribute("КНД")!.Value, Is.EqualTo("1115132"));
                Assert.That(document.Attribute("УИД")!.Value, Is.EqualTo("0000000000-7777777777-000001"));
                Assert.That(document.Attribute("ДатаИнфПок")!.Value, Is.EqualTo("03.04.2026"));
                Assert.That(document.Attribute("ВремИнфПок")!.Value, Is.EqualTo("09.30.00"));
                Assert.That(document.Attribute("НаимЭконСубСост")!.Value, Is.EqualTo("ООО «Покупатель», ИНН/КПП: 0000000000/000001001"));

                Assert.That(sellerFile.Attribute("ИдФайлИнфПр")!.Value, Is.EqualTo(SellerFileName));
                Assert.That(sellerFile.Attribute("ДатаФайлИнфПр")!.Value, Is.EqualTo("02.04.2026"));
                Assert.That(sellerFile.Attribute("ВремФайлИнфПр")!.Value, Is.EqualTo("10.15.00"));
                Assert.That(sellerFile.Element("ЭП")!.Value, Is.EqualTo(Convert.ToBase64String(signature)));

                Assert.That(content.Attribute("НаимДокОпрПр")!.Value, Does.StartWith("Счет-фактура и документ об отгрузке"));
                Assert.That(content.Attribute("Функция")!.Value, Is.EqualTo("СЧФДОП"));
                Assert.That(content.Attribute("ПорНомДокИнфПр")!.Value, Is.EqualTo("15"));
                Assert.That(content.Attribute("ДатаДокИнфПр")!.Value, Is.EqualTo("01.04.2026"));
                Assert.That(content.Attribute("ВидОпер")!.Value, Is.EqualTo("Поставка"));

                Assert.That(acceptance.Attribute("СодОпер")!.Value, Is.EqualTo("Товары приняты с расхождениями"));
                Assert.That(acceptance.Attribute("ДатаПрин")!.Value, Is.EqualTo("03.04.2026"));
                Assert.That(acceptance.Element("КодСодОпер")!.Attribute("КодИтога")!.Value, Is.EqualTo("2"));
                Assert.That(acceptance.Element("КодСодОпер")!.Element("РеквДокРасх")!.Attribute("РеквНомерДок")!.Value, Is.EqualTo("Р-1"));
                Assert.That(acceptance.Element("СвЛицПрин")!.Element("РабОргПок")!.Attribute("Должность")!.Value, Is.EqualTo("Кладовщик"));

                Assert.That(document.Element("Подписант")!.Attribute("СпосПодтПолном")!.Value, Is.EqualTo("1"));
                Assert.That(document.Elements().Select(x => x.Name.LocalName), Is.EqualTo(new[] { "ИдИнфПрод", "СодФХЖ4", "Подписант" }));
            });
        }

        [Test]
        public void ReceiptUniversalTransferDocument_SellerFileNotFormat970_ThrowsBeforeSigning()
        {
            // Arrange
            const string oldFormatFileName = "ON_NSCHFDOPPR_2BM-0000000000-000000000_2BM-7777777777-777777777_20260402_" + DocumentId;
            var sellerXml = CreateSellerXml(null).Replace($"ИдФайл=\"{SellerFileName}\"", $"ИдФайл=\"{oldFormatFileName}\"");
            var handler = new StubHttpMessageHandler(CreateSellerArchiveResponse(sellerXml, [0x30]));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var ex = Assert.ThrowsAsync<InvalidDataException>(() => edoProvider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), CreateBuyerUniversalTransferDocument()));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Does.Contain(oldFormatFileName).And.Contain("970"));
                Assert.That(handler.Requests, Has.Count.EqualTo(1));
                Assert.That(edoProvider.SignedContents, Is.Empty);
            });
        }

        [Test]
        public void ReceiptUniversalTransferDocument_SellerInvoiceWithoutDocumentName_ThrowsBeforeSigning()
        {
            // Arrange
            var sellerXml = Regex.Replace(CreateSellerXml(null).Replace("Функция=\"СЧФДОП\"", "Функция=\"СЧФ\""), " НаимДокОпр=\"[^\"]+\"", "");
            var handler = new StubHttpMessageHandler(CreateSellerArchiveResponse(sellerXml, [0x30]));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var ex = Assert.ThrowsAsync<InvalidDataException>(() => edoProvider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), CreateBuyerUniversalTransferDocument()));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Does.Contain(SellerFileName).And.Contain("НаимДокОпр"));
                Assert.That(handler.Requests, Has.Count.EqualTo(1));
                Assert.That(edoProvider.SignedContents, Is.Empty);
            });
        }

        [Test]
        public void ReceiptUniversalTransferDocument_ServiceError_ThrowsWithPostedTitle()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(
                CreateSellerArchiveResponse(CreateSellerXml(null), [0x30]),
                CreateResponse(HttpStatusCode.UnprocessableEntity, """{"error_message":"Документ уже подписан"}""", "application/json"));
            using var edoProvider = new FakeSigningEdoProvider(new HttpClient(handler), new FakeTokenProvider(), SignatureBase64);

            // Act
            var ex = Assert.ThrowsAsync<EdoPostDocumentException>(() => edoProvider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), CreateBuyerUniversalTransferDocument()));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex!.Message, Is.EqualTo("Документ уже подписан"));
                Assert.That(((HttpRequestException)ex.InnerException!).StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
                Assert.That(ex.RawContent, Is.EqualTo(handler.Requests[1].Parts![0].Content));
                Assert.That(XDocument.Parse(ex.Content).Root!.Attribute("ИдФайл")!.Value, Is.EqualTo(ex.FileId));
            });
        }

        [Test]
        public async Task Unauthorized_RefreshesTokenOnce()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(
                CreateResponse(HttpStatusCode.Unauthorized, """{"error_message":"Токен не действителен. Необходимо получить новый токен аутентификации"}""", "application/json"),
                CreateResponse(HttpStatusCode.OK, """{"items":[],"has_next_page":false}""", "application/json"));
            var tokenProvider = new FakeTokenProvider();
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, tokenProvider);

            // Act
            var data = await edoProvider.GetIncomingDocumentListAsync(CreateSettings());

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(data.HasNextPage, Is.False);
                Assert.That(tokenProvider.ExpiredCount, Is.EqualTo(1));
                Assert.That(handler.Requests.Select(x => x.Authorization), Is.EqualTo(new[] { "Bearer uuid-1", "Bearer uuid-2" }));
            });
        }

        [Test]
        public void Unauthorized_AfterRefresh_ThrowsWithoutLoop()
        {
            // Arrange
            var handler = new StubHttpMessageHandler(
                CreateResponse(HttpStatusCode.Unauthorized, """{"error_message":"unauthorized"}""", "application/json"),
                CreateResponse(HttpStatusCode.Unauthorized, """{"error_message":"unauthorized"}""", "application/json"));
            var tokenProvider = new FakeTokenProvider();
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, tokenProvider);

            // Act
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => edoProvider.GetIncomingDocumentAsync(CreateSettings(), "id"));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(ex!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(ex.Message, Is.EqualTo("unauthorized"));
                Assert.That(handler.Requests, Has.Count.EqualTo(2));
                Assert.That(tokenProvider.ExpiredCount, Is.EqualTo(1));
            });
        }

        [TestCase("""{"error_message":"Документ не найден в ГИС МТ"}""", "Документ не найден в ГИС МТ")]
        [TestCase("""{"code":500,"error_message":"Внутренняя ошибка"}""", "Внутренняя ошибка")]
        [TestCase("""{"errors":[{"error_message":"Первая"},{"error_message":"Вторая"}]}""", "Первая; Вторая")]
        public void Error_UsesServiceMessage(string errorJson, string expectedMessage)
        {
            // Arrange
            var handler = new StubHttpMessageHandler(CreateResponse(HttpStatusCode.NotFound, errorJson, "application/json"));
            using var edoProvider = new TrueApiEdoProvider(new HttpClient(handler), true, new FakeTokenProvider());

            // Act
            var ex = Assert.ThrowsAsync<HttpRequestException>(() => edoProvider.GetIncomingDocumentAsync(CreateSettings(), "id"));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo(expectedMessage));
            Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        private sealed record StubRequest(HttpMethod Method, Uri Uri, string? Authorization, string Accept, string? ContentType, string? ContentEncoding, string? Body, List<StubPart>? Parts);

        private sealed record StubPart(string? Name, string? FileName, string? ContentType, string? ContentEncoding, byte[] Content);

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;

            public StubHttpMessageHandler(params HttpResponseMessage[] responses)
            {
                _responses = new Queue<HttpResponseMessage>(responses);
            }

            public List<StubRequest> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Тело читаем здесь: после отправки провайдер освобождает запрос вместе с телом.
                var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

                List<StubPart>? parts = null;
                if (request.Content is MultipartContent multipart)
                {
                    parts = [];
                    foreach (var part in multipart)
                    {
                        var disposition = part.Headers.ContentDisposition;
                        parts.Add(new StubPart(disposition?.Name?.Trim('"'), disposition?.FileName?.Trim('"'), part.Headers.ContentType?.MediaType,
                            part.Headers.ContentEncoding.ToString(), await part.ReadAsByteArrayAsync(cancellationToken)));
                    }
                }

                Requests.Add(new StubRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), request.Headers.Accept.ToString(),
                    request.Content?.Headers.ContentType?.MediaType, request.Content?.Headers.ContentEncoding.ToString(), body, parts));

                return _responses.Dequeue();
            }
        }

        /// <summary>
        /// Подпись без КриптоПро: запоминает подписанные байты и возвращает заданную подпись.
        /// </summary>
        private sealed class FakeSigningEdoProvider : TrueApiEdoProvider
        {
            private readonly string _signature;

            public FakeSigningEdoProvider(HttpClient httpClient, ITrueApiTokenProvider tokenProvider, string signature) : base(httpClient, true, tokenProvider)
            {
                _signature = signature;
            }

            public List<byte[]> SignedContents { get; } = [];

            public string? Thumbprint { get; private set; }

            protected override string SignDetached(TrueApiEdoProviderOption settings, byte[] content)
            {
                Thumbprint = settings.CertificateThumbprint;
                SignedContents.Add(content);

                return _signature;
            }
        }

        private sealed class FakeTokenProvider : ITrueApiTokenProvider
        {
            private int _issuedCount;

            public int ExpiredCount { get; private set; }

            public TrueApiTokenProviderOption? LastOption { get; private set; }

            public Task<UnitedTokenModel> GetTokenAsync(TrueApiTokenProviderOption settings)
            {
                LastOption = settings;
                _issuedCount++;

                return Task.FromResult(new UnitedTokenModel { UuidToken = $"uuid-{_issuedCount}", Token = "jwt" });
            }

            public void SetTokenExpired() => ExpiredCount++;
        }
    }
}
