using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using CIS.EDM.CRPT.Helpers;
using CIS.EDM.CRPT.Models;
using CIS.EDM.CRPT.Providers;
using CIS.EDM.Helpers;
using CIS.EDM.Models;
using CIS.EDM.Models.Reference;
using Microsoft.Extensions.Logging.Abstractions;
using Spoleto.TrueApi.Auth.Models;
using Spoleto.TrueApi.Auth.Models.Options;
using Spoleto.TrueApi.Auth.Providers;
using Spoleto.TrueApi.Edo.Models.Options;
using Spoleto.TrueApi.Edo.Providers;
using VB = CIS.EDM.Models.V5_03.Buyer;
using VS = CIS.EDM.Models.V5_03.Seller;

namespace CIS.EDM.CRPT.Tests
{
    /// <summary>
    /// <see cref="TrueApiCRPTProvider"/> на подставном транспорте: перевод моделей, адреса True API и сверка с <see cref="CRPTProvider"/>. Стенд не нужен.
    /// </summary>
    /// <remarks>
    /// XML сверяется со старым генератором <see cref="XmlHelperV5_03"/>, сведения о файле продавца - со старым разбором <see cref="XmlParserV5_03"/>.
    /// Свойство, добавленное в модель CIS.EDM, но забытое в <see cref="TrueApiEdoMapper"/>, даст расхождение.
    /// </remarks>
    public class TrueApiCRPTProviderTests
    {
        private const string AuthUrl = "https://markirovka.sandbox.crptech.ru";

        private const string DocumentId = "220262cf-0fe7-4d3f-996a-038181ba7812";

        private const string PostedId = "33a333ee-3cb3-3aae-3f33-d33ce333333c";

        // Подпись, которую подставной провайдер «ЭДО Лайт» возвращает вместо КриптоПро.
        private const string SignatureBase64 = "MIIPkQYJKoZIhvcNAQcC";

        // Пример ответа из описания метода 9.2.8 True API v.746.0: у исходящего документа - получатель, суммы и тип строками.
        private const string OutgoingDocumentListJson = """
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
                      "number": "15",
                      "processed_at": 1776253170,
                      "status": 3,
                      "total_price": 100,
                      "total_vat_amount": 0,
                      "type": 520,
                      "is_title_signed": false
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
                    "status": "REGISTERED"
                  },
                  "status": 1,
                  "total_price": "1930500",
                  "total_vat_amount": "279500",
                  "type": "504",
                  "create_time_stamp": 1582059600,
                  "export_time_stamp": 1582097180
                }
              ],
              "has_next_page": true
            }
            """;

        // Даты документов. Суммы и тип - числами: CIS.EDM строки на их месте не читает.
        private const string DocumentListDatesJson = """
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
                      "processed_at": 1776253170,
                      "status": 3,
                      "type": 520
                    }
                  ],
                  "status": 1,
                  "type": 504,
                  "create_time_stamp": 1582059600,
                  "export_time_stamp": 1582097180
                }
              ],
              "has_next_page": false
            }
            """;

        // Открепленная подпись файла продавца в архиве документа.
        private static readonly byte[] SellerSignature = [0x30, 0x82, 0x0F, 0x91, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x02];

        static TrueApiCRPTProviderTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//support for "windows-1251"
        }

        private static CRPTOption CreateSettings() => new()
        {
            // Адрес прежнего API «ЭДО Лайт»: провайдеру True API он не нужен.
            ServiceUrl = "https://elk.edo.crpt.tech",
            AuthUrl = AuthUrl,
            CertificateThumbprint = "thumbprint",
            Inn = "7777777777"
        };

        private static (FakeSigningCRPTProvider Provider, StubHttpMessageHandler Handler, FakeTokenProvider TokenProvider) CreateProvider(params HttpResponseMessage[] responses)
        {
            var handler = new StubHttpMessageHandler(responses);
            var tokenProvider = new FakeTokenProvider();

            return (new FakeSigningCRPTProvider(new StubHttpClientFactory(handler), tokenProvider), handler, tokenProvider);
        }

        private static HttpResponseMessage CreateJsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
            => new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        private static HttpResponseMessage CreatePostedResponse() => CreateJsonResponse($$"""{"id":"{{PostedId}}"}""");

        private static HttpResponseMessage CreateFileResponse(byte[] content) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(content) };

        /// <summary>
        /// Архив документа «ЭДО Лайт»: файл информации продавца и его подпись.
        /// </summary>
        private static HttpResponseMessage CreateSellerArchiveResponse(DocumentData sellerFile)
        {
            using var archiveStream = new MemoryStream();
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
            {
                AddEntry(archive, sellerFile.Id + ".xml", sellerFile.FileEncoding.GetBytes(sellerFile.Content));
                AddEntry(archive, sellerFile.Id + ".p7s", SellerSignature);
            }

            return CreateFileResponse(archiveStream.ToArray());
        }

        private static void AddEntry(ZipArchive archive, string name, byte[] content)
        {
            using var entryStream = archive.CreateEntry(name).Open();
            entryStream.Write(content);
        }

        private static SignedDocumentInfo CreateSignedDocumentInfo(DocumentData sellerFile) => new()
        {
            Content = sellerFile.Content,
            DetachedSignature = Convert.ToBase64String(SellerSignature)
        };

        private static string CreateListJson(bool hasNextPage, params (string Id, long CreatedAt)[] items)
            => JsonSerializer.Serialize(new
            {
                items = items.Select(x => new { id = x.Id, created_at = x.CreatedAt, status = 3, type = 520, documents = Array.Empty<object>() }),
                has_next_page = hasNextPage
            });

        // Где старый генератор покупателя расходится с форматом 970, новый пишет по формату:
        // ВидОпер вместо ВидОперации, РеквДокРасх внутри КодСодОпер, без пустого Должн у подписанта без должности
        // и без пробела перед запятой в НаимЭконСубСост у ИП и физлица без отчества.
        private static string FixOldBuyerXml(string xml)
        {
            var doc = XDocument.Parse(xml);
            var document = doc.Root!.Element("Документ")!;
            var creatorName = document.Attribute("НаимЭконСубСост")!;
            creatorName.Value = creatorName.Value.Replace(" , ИНН", ", ИНН");
            document.Elements("Подписант").Attributes("Должн").Where(x => x.Value == "").Remove();

            var content = document.Element("СодФХЖ4")!;
            content.ReplaceAttributes(content.Attributes().Select(x => x.Name == "ВидОперации" ? new XAttribute("ВидОпер", x.Value) : x).ToList());

            var acceptance = content.Element("СвПрин")!;
            if (acceptance.Element("РеквДокРасх") is { } discrepancy)
            {
                discrepancy.Remove();
                acceptance.Element("КодСодОпер")!.Add(discrepancy);
            }

            return doc.Declaration + Environment.NewLine + doc;
        }

        private static IEnumerable<TestCaseData> SellerDocuments()
        {
            yield return new TestCaseData(UniversalTransferDocumentSamples.Full()).SetArgDisplayNames("СЧФДОП, полный");
            yield return new TestCaseData(UniversalTransferDocumentSamples.Others()).SetArgDisplayNames("СЧФ, УПДи, остальные ветки");
            yield return new TestCaseData(UniversalTransferDocumentSamples.Dop()).SetArgDisplayNames("ДОП, ФЛПер");
        }

        private static IEnumerable<TestCaseData> BuyerDocuments()
        {
            yield return new TestCaseData(UniversalTransferDocumentSamples.BuyerFull(), UniversalTransferDocumentSamples.Full(), false)
                .SetArgDisplayNames("принято, РабОргПок, госзакупки, МЧД");
            yield return new TestCaseData(UniversalTransferDocumentSamples.BuyerOrganizationPerson(), UniversalTransferDocumentSamples.Dop(), false)
                .SetArgDisplayNames("с расхождениями, ПредОргПрин, бумажная доверенность");
            yield return new TestCaseData(UniversalTransferDocumentSamples.BuyerPhysicalPerson(), UniversalTransferDocumentSamples.Full(), false)
                .SetArgDisplayNames("не принято, ФЛПрин");
            yield return new TestCaseData(UniversalTransferDocumentSamples.BuyerKnownDifferences(), UniversalTransferDocumentSamples.OthersWithBuyerTitle(), true)
                .SetArgDisplayNames("известные отличия, УПДи");
        }

        [TestCaseSource(nameof(SellerDocuments))]
        public async Task PostUniversalTransferDocument_SellerModel_XmlEqualsCRPTProvider(VS.SellerUniversalTransferDocument document)
        {
            // Arrange
            var expectedXml = XmlHelperV5_03.GenerateXml(document).Content;
            var (provider, handler, _) = CreateProvider(CreatePostedResponse());
            var path = string.IsNullOrEmpty(document.RevisionNumber) ? "/api/v3/true-api/edo/outgoing-documents" : "/api/v3/true-api/edo/outgoing-documents/xml/updi";

            // Act
            var result = await provider.PostUniversalTransferDocumentAsync(CreateSettings(), document, isDraft: true);

            // Assert
            var request = handler.Requests.Single();
            Assert.Multiple(() =>
            {
                Assert.That(result.Exception, Is.Null);
                Assert.That(result.Id, Is.EqualTo(PostedId));
                Assert.That(result.Content, Is.EqualTo(expectedXml));
                Assert.That(request.Uri.ToString(), Is.EqualTo(AuthUrl + path));
                Assert.That(request.Parts.Select(x => x.Name), Is.EqualTo(new[] { "content" }), "Черновик уходит без подписи.");
            });
        }

        [TestCaseSource(nameof(BuyerDocuments))]
        public async Task ReceiptUniversalTransferDocument_BuyerModel_XmlEqualsCRPTProvider(VB.BuyerUniversalTransferDocument document, VS.SellerUniversalTransferDocument seller, bool fixOldXml)
        {
            // Arrange
            var sellerFile = XmlHelperV5_03.GenerateXml(seller);

            // CRPTProvider: сведения о файле продавца - из XmlParserV5_03, имя файла покупателя - из модели.
            document.SellerDocumentInfo = XmlParserV5_03.SellerInfoFromXml(CreateSignedDocumentInfo(sellerFile));
            var oldFile = XmlHelperV5_03.GenerateXml(document);
            var expectedXml = fixOldXml ? FixOldBuyerXml(oldFile.Content) : oldFile.Content;

            var (provider, handler, _) = CreateProvider(CreateSellerArchiveResponse(sellerFile), CreatePostedResponse());
            var titlePath = string.IsNullOrEmpty(seller.RevisionNumber) ? "/xml/upd/title/970" : "/xml/updi/title/970";

            // Act
            var result = await provider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), document);

            // Assert
            Assert.That(result.Exception, Is.Null);

            // Имя файла покупателя TrueApiEdoProvider формирует сам: по имени файла продавца и с новым GUID.
            var fileIdRegex = new Regex(" ИдФайл=\"([^\"]+)\"");
            var fileId = fileIdRegex.Match(result.Content).Groups[1].Value;
            var xml = fileIdRegex.Replace(result.Content, $" ИдФайл=\"{oldFile.Id}\"", 1);

            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(PostedId));
                Assert.That(xml, Is.EqualTo(expectedXml));
                Assert.That(fileId, Does.StartWith("ON_NSCHFDOPPOK_2LT-2_2LT-1_"));
                Assert.That(handler.Requests.Select(x => $"{x.Method} {x.Uri}"), Is.EqualTo(new[]
                {
                    $"GET {AuthUrl}/api/v3/true-api/elk/incoming-documents/{document.EdmDocumentId}",
                    $"POST {AuthUrl}/api/v3/true-api/edo/incoming-documents{titlePath}"
                }));
                Assert.That(handler.Requests[1].Parts.Select(x => x.Name), Is.EqualTo(new[] { "content", "doc_id", "signature" }));
                Assert.That(Encoding.UTF8.GetString(handler.Requests[1].Parts[1].Content), Is.EqualTo(document.EdmDocumentId));
            });
        }

        [Test]
        public async Task PostUniversalTransferDocument_ServiceError_ReturnsExceptionInResult()
        {
            // Arrange
            var document = UniversalTransferDocumentSamples.Dop();
            var (provider, _, _) = CreateProvider(CreateJsonResponse("""{"error_message":"Файл с таким именем уже загружен"}""", HttpStatusCode.BadRequest));

            // Act
            var result = await provider.PostUniversalTransferDocumentAsync(CreateSettings(), document, isDraft: true);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.Null);
                Assert.That(result.Content, Is.EqualTo(XmlHelperV5_03.GenerateXml(document).Content), "Как у CRPTProvider: XML, который не принял сервис, вызывающий код пишет в журнал.");
                Assert.That(result.Exception, Is.TypeOf<HttpRequestException>());
                Assert.That(((HttpRequestException)result.Exception).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            });
        }

        [Test]
        public async Task ReceiptUniversalTransferDocument_ServiceError_ReturnsExceptionInResult()
        {
            // Arrange
            var sellerFile = XmlHelperV5_03.GenerateXml(UniversalTransferDocumentSamples.Full());
            var (provider, handler, _) = CreateProvider(CreateSellerArchiveResponse(sellerFile), CreateJsonResponse("""{"error_message":"Документ уже подписан"}""", HttpStatusCode.BadRequest));

            // Act
            var result = await provider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), UniversalTransferDocumentSamples.BuyerFull());

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.Null);
                Assert.That(result.Content, Is.EqualTo(Encoding.GetEncoding("windows-1251").GetString(handler.Requests[1].Parts[0].Content)));
                Assert.That(result.Content, Does.Contain(" ИдФайл=\"ON_NSCHFDOPPOK_"));
                Assert.That(result.Exception, Is.TypeOf<HttpRequestException>());
            });
        }

        /// <summary>
        /// Файл продавца не скачался - XML титула ещё нет, и в <see cref="ResultInfo.Content"/> пустая строка, а не <see langword="null"/>: журнал не принимает null.
        /// </summary>
        [Test]
        public async Task ReceiptUniversalTransferDocument_SellerFileError_ReturnsEmptyContent()
        {
            // Arrange
            var (provider, handler, _) = CreateProvider(CreateJsonResponse("""{"error_message":"Документ не найден"}""", HttpStatusCode.NotFound));

            // Act
            var result = await provider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), UniversalTransferDocumentSamples.BuyerFull());

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.Null);
                Assert.That(result.Content, Is.Empty);
                Assert.That(((HttpRequestException)result.Exception).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(handler.Requests, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public void Format820_ThrowsNotSupportedWithoutRequests()
        {
            // Arrange
            var (provider, handler, _) = CreateProvider();

            // Act & Assert
            Assert.Multiple(() =>
            {
                Assert.ThrowsAsync<NotSupportedException>(async () => await provider.PostUniversalTransferDocumentAsync(CreateSettings(), new CIS.EDM.Models.V5_01.Seller.SellerUniversalTransferDocument()));
                Assert.ThrowsAsync<NotSupportedException>(async () => await provider.ReceiptUniversalTransferDocumentAsync(CreateSettings(), new CIS.EDM.Models.V5_01.Buyer.BuyerUniversalTransferDocument()));
                Assert.That(handler.Requests, Is.Empty);
            });
        }

        [Test]
        public async Task GetOutgoingDocumentList_MapsSearchModelAndDocuments()
        {
            // Arrange
            var (provider, handler, tokenProvider) = CreateProvider(CreateJsonResponse(OutgoingDocumentListJson));
            var createdFrom = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            var searchModel = new DocumentCollectionSearchModel
            {
                Limit = 10,
                Offset = 20,
                PartnerInn = "0000000000",
                CreatedFrom = createdFrom,
                Status = [DocumentState.Sent],
                Type = [DocumentType.UPDWithSCHFDOPInvoiceWaybill],
                SortBy = SortField.created_at
            };

            // Act
            var result = await provider.GetOutgoingDocumentListAsync(CreateSettings(), searchModel);

            // Assert
            var request = handler.Requests.Single();
            var query = HttpUtility.ParseQueryString(request.Uri.Query);
            var group = result.Items.Single();
            var document = group.Documents.Single();
            Assert.Multiple(() =>
            {
                Assert.That(request.Uri.GetLeftPart(UriPartial.Path), Is.EqualTo(AuthUrl + "/api/v3/true-api/elk/outgoing-documents"));
                Assert.That(tokenProvider.LastOption!.ServiceUrl, Is.EqualTo(AuthUrl), "Токен запрашивается там же, где методы «ЭДО Лайт».");
                Assert.That(query["limit"], Is.EqualTo("10"));
                Assert.That(query["offset"], Is.EqualTo("20"));
                Assert.That(query["partner_inn"], Is.EqualTo("0000000000"));
                Assert.That(query["created_from"], Is.EqualTo(new DateTimeOffset(createdFrom).ToUnixTimeSeconds().ToString()));
                Assert.That(query["status"], Is.EqualTo("1"));
                Assert.That(query["type"], Is.EqualTo("504"));
                Assert.That(query["sortBy"], Is.EqualTo("created_at"));

                Assert.That(result.HasNextPage, Is.True);
                Assert.That(group.Id, Is.EqualTo("33a333ee-3cb3-3aae-3f33-d33ce333333c"));
                Assert.That(group.GroupId, Is.EqualTo("4d44444e-4d44-4444-4e44-a4cd444ed444"));
                Assert.That(group.Status, Is.EqualTo(DocumentState.Sent));
                Assert.That(group.Type, Is.EqualTo(DocumentType.UPDWithSCHFDOPInvoiceWaybill));
                Assert.That(group.TotalPrice, Is.EqualTo(1930500M));
                Assert.That(group.TotalVatAmount, Is.EqualTo(279500M));
                Assert.That(group.Sender.Id, Is.EqualTo(999999999), "У исходящих контрагент - получатель.");
                Assert.That(group.Sender.Inn, Is.EqualTo("0000000000"));
                Assert.That(group.Sender.Name, Is.EqualTo("ООО"));

                Assert.That(document.Id, Is.EqualTo(DocumentId));
                Assert.That(document.Number, Is.EqualTo("15"));
                Assert.That(document.Status, Is.EqualTo(DocumentState.DeliveredSignatureRequired));
                Assert.That(document.Type, Is.EqualTo(DocumentType.UPDWithDOPWaybill970));
                Assert.That(document.TotalPrice, Is.EqualTo(100M));
                Assert.That(document.IsTitleSigned, Is.False);
            });
        }

        [TestCase(DateTimeKind.Utc)]
        [TestCase(DateTimeKind.Unspecified)]
        [TestCase(DateTimeKind.Local)]
        public async Task GetIncomingDocumentList_DatesAsCRPTProvider(DateTimeKind kind)
        {
            // Arrange
            var (provider, handler, _) = CreateProvider(CreateJsonResponse(DocumentListDatesJson));
            var searchModel = new DocumentCollectionSearchModel
            {
                CreatedFrom = new DateTime(2026, 10, 30, 0, 0, 0, kind),
                CreatedTo = new DateTime(2026, 10, 30, 23, 59, 59, kind)
            };

            // CRPTProvider: критерии - HttpHelper.ToQueryString, ответ - HttpHelper.FromJson с конвертерами CIS.EDM.
            var expectedQuery = HttpUtility.ParseQueryString(HttpHelper.ToQueryString(searchModel));
            var expectedGroup = HttpHelper.FromJson<DocumentCollection>(DocumentListDatesJson).Items.Single();

            // Act
            var result = await provider.GetIncomingDocumentListAsync(CreateSettings(), searchModel);

            // Assert
            var query = HttpUtility.ParseQueryString(handler.Requests.Single().Uri.Query);
            var group = result.Items.Single();
            Assert.Multiple(() =>
            {
                Assert.That(query["created_from"], Is.EqualTo(expectedQuery["created_from"]));
                Assert.That(query["created_to"], Is.EqualTo(expectedQuery["created_to"]));
                Assert.That(GetDates(group), Is.EqualTo(GetDates(expectedGroup)));
                Assert.That(GetDates(group.Documents.Single()), Is.EqualTo(GetDates(expectedGroup.Documents.Single())));
            });
        }

        // Строкой "o": равные DateTime с разным Kind пишутся по-разному.
        private static string[] GetDates(Document x) => [x.CreatedAt.ToString("o"), x.Date.ToString("o"), x.CreateTimestamp.ToString("o"), x.ExportTimestamp.ToString("o")];

        private static string[] GetDates(DocumentInfo x) => [x.CreatedAt.ToString("o"), x.Date.ToString("o"), x.ProcessedAt.ToString("o")];

        [Test]
        public async Task GetIncomingDocumentList_SeveralStatuses_MergesRequestsNewestFirst()
        {
            // Arrange
            var (provider, handler, _) = CreateProvider(
                CreateJsonResponse(CreateListJson(false, ("a", 100), ("c", 300))),
                CreateJsonResponse(CreateListJson(true, ("b", 200), ("c", 300))));
            var searchModel = new DocumentCollectionSearchModel { Limit = 2, Status = [DocumentState.DeliveredSignatureRequired, DocumentState.ReviewedSignatureRequired] };

            // Act
            var result = await provider.GetIncomingDocumentListAsync(CreateSettings(), searchModel);

            // Assert
            var queries = handler.Requests.Select(x => HttpUtility.ParseQueryString(x.Uri.Query)).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(queries.Select(x => x["status"]), Is.EqualTo(new[] { "3", "13" }));
                Assert.That(queries.Select(x => x["limit"]), Is.EqualTo(new[] { "2", "2" }));
                Assert.That(result.Items.Select(x => x.Id), Is.EqualTo(new[] { "c", "b", "a" }), "Новые первыми, без повторов.");
                Assert.That(result.HasNextPage, Is.True);
            });
        }

        /// <summary>
        /// Все типы - один запрос без отбора по типу, а не запрос на каждый тип. Документ типа, которого нет в CIS.EDM (договор с оператором ГИС МТ), отбрасывается.
        /// </summary>
        [Test]
        public async Task GetIncomingDocumentList_AllTypes_OneRequestWithoutTypeFilter()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                items = new[]
                {
                    new { id = "upd", created_at = 200, status = 3, type = 520, documents = Array.Empty<object>() },
                    new { id = "contract", created_at = 100, status = 3, type = 9999, documents = Array.Empty<object>() }
                },
                has_next_page = false
            });
            var (provider, handler, _) = CreateProvider(CreateJsonResponse(json));
            var searchModel = new DocumentCollectionSearchModel { Limit = 100, Status = [DocumentState.DeliveredSignatureRequired], Type = Enum.GetValues<DocumentType>().ToList() };

            // Act
            var result = await provider.GetIncomingDocumentListAsync(CreateSettings(), searchModel);

            // Assert
            var query = HttpUtility.ParseQueryString(handler.Requests.Single().Uri.Query);
            Assert.Multiple(() =>
            {
                Assert.That(query["type"], Is.Null);
                Assert.That(query["status"], Is.EqualTo("3"));
                Assert.That(result.Items.Select(x => x.Id), Is.EqualTo(new[] { "upd" }));
            });
        }

        [Test]
        public void GetIncomingDocumentList_SortByDate_ThrowsNotSupportedWithoutRequests()
        {
            // Arrange
            var (provider, handler, _) = CreateProvider();
            var searchModel = new DocumentCollectionSearchModel { SortBy = SortField.date };

            // Act & Assert
            Assert.ThrowsAsync<NotSupportedException>(async () => await provider.GetIncomingDocumentListAsync(CreateSettings(), searchModel));
            Assert.That(handler.Requests, Is.Empty);
        }

        [TestCase("windows-1251")]
        [TestCase("utf-8")]
        public async Task GetIncomingDocument_Xml_ReadsInEncodingFromDeclaration(string encodingName)
        {
            // Arrange
            var xml = $"""<?xml version="1.0" encoding="{encodingName}"?><Файл ИдФайл="ON_NSCHFDOPPR_1" НаимОтпр="ООО «Продавец»"/>""";
            var (provider, handler, _) = CreateProvider(CreateFileResponse(Encoding.GetEncoding(encodingName).GetBytes(xml)));

            // Act
            var result = await provider.GetIncomingDocumentAsync(CreateSettings(), DocumentId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(xml));
                Assert.That(handler.Requests.Single().Uri.ToString(), Is.EqualTo($"{AuthUrl}/api/v3/true-api/elk/incoming-documents/{DocumentId}/content"));
            });
        }

        [Test]
        public async Task GetIncomingSignedDocumentInfo_ReturnsXmlAndSignatureInBase64()
        {
            // Arrange
            var sellerFile = XmlHelperV5_03.GenerateXml(UniversalTransferDocumentSamples.Dop());
            var (provider, handler, _) = CreateProvider(CreateSellerArchiveResponse(sellerFile));

            // Act
            var result = await provider.GetIncomingSignedDocumentInfoAsync(CreateSettings(), DocumentId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(CreateSignedDocumentInfo(sellerFile)));
                Assert.That(handler.Requests.Single().Uri.ToString(), Is.EqualTo($"{AuthUrl}/api/v3/true-api/elk/incoming-documents/{DocumentId}"));
            });
        }

        [TestCaseSource(nameof(SellerDocuments))]
        public async Task GetIncomingSellerDocumentInfo_EqualsCRPTProvider(VS.SellerUniversalTransferDocument seller)
        {
            // Arrange
            var sellerFile = XmlHelperV5_03.GenerateXml(seller);
            var expected = XmlParserV5_03.SellerInfoFromXml(CreateSignedDocumentInfo(sellerFile));
            var (provider, _, _) = CreateProvider(CreateSellerArchiveResponse(sellerFile));

            // Act
            var result = await provider.GetIncomingSellerDocumentInfoAsync(CreateSettings(), DocumentId);

            // Assert
            Assert.That(JsonSerializer.Serialize(result), Is.EqualTo(JsonSerializer.Serialize(expected)));
        }

        [Test]
        public async Task SignOutgoingDocument_SignsDraftFileAndPostsSignature()
        {
            // Arrange
            var draft = Encoding.GetEncoding("windows-1251").GetBytes("""<?xml version="1.0" encoding="windows-1251"?><Файл ИдФайл="ON_NSCHFDOPPR_1"/>""");
            var (provider, handler, _) = CreateProvider(CreateFileResponse(draft), new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            await provider.SignOutgoingDocumentAsync(CreateSettings(), DocumentId);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(handler.Requests.Select(x => $"{x.Method} {x.Uri}"), Is.EqualTo(new[]
                {
                    $"GET {AuthUrl}/api/v3/true-api/elk/outgoing-documents/{DocumentId}/content",
                    $"POST {AuthUrl}/api/v3/true-api/edo/outgoing-documents/{DocumentId}/signature"
                }));
                Assert.That(provider.SignedContents.Single(), Is.EqualTo(draft));
                Assert.That(handler.Requests[1].Body, Is.EqualTo(SignatureBase64));
            });
        }

        private sealed record StubPart(string Name, byte[] Content);

        private sealed record StubRequest(HttpMethod Method, Uri Uri, string Body, List<StubPart> Parts);

        private sealed class StubHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses = new(responses);

            public List<StubRequest> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Тело читаем здесь: после отправки провайдер освобождает запрос вместе с телом.
                var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

                var parts = new List<StubPart>();
                if (request.Content is MultipartContent multipart)
                {
                    foreach (var part in multipart)
                    {
                        parts.Add(new StubPart(part.Headers.ContentDisposition?.Name?.Trim('"'), await part.ReadAsByteArrayAsync(cancellationToken)));
                    }
                }

                Requests.Add(new StubRequest(request.Method, request.RequestUri!, body, parts));

                return _responses.Count > 0 ? _responses.Dequeue() : throw new InvalidOperationException($"Нет ответа на {request.Method} {request.RequestUri}.");
            }
        }

        private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(handler, false);
        }

        /// <summary>
        /// Провайдер, который подписывает без КриптоПро: вместо подписи - <see cref="SignatureBase64"/>.
        /// </summary>
        private sealed class FakeSigningCRPTProvider(IHttpClientFactory httpClientFactory, ITrueApiTokenProvider tokenProvider)
            : TrueApiCRPTProvider(NullLogger<TrueApiCRPTProvider>.Instance, httpClientFactory, tokenProvider)
        {
            public List<byte[]> SignedContents { get; } = [];

            protected override TrueApiEdoProvider CreateEdoProvider(HttpClient httpClient) => new FakeSigningEdoProvider(httpClient, tokenProvider, SignedContents);
        }

        private sealed class FakeSigningEdoProvider(HttpClient httpClient, ITrueApiTokenProvider tokenProvider, List<byte[]> signedContents)
            : TrueApiEdoProvider(httpClient, false, tokenProvider)
        {
            protected override string SignDetached(TrueApiEdoProviderOption settings, byte[] content)
            {
                signedContents.Add(content);

                return SignatureBase64;
            }
        }

        private sealed class FakeTokenProvider : ITrueApiTokenProvider
        {
            public TrueApiTokenProviderOption LastOption { get; private set; }

            public Task<UnitedTokenModel> GetTokenAsync(TrueApiTokenProviderOption settings)
            {
                LastOption = settings;

                return Task.FromResult(new UnitedTokenModel { UuidToken = "uuid", Token = "jwt" });
            }

            public void SetTokenExpired()
            {
            }
        }
    }
}
