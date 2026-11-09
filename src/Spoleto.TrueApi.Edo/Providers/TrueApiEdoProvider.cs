using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CIS.Cryptography;
using Spoleto.Common.Helpers;
using Spoleto.RestClient;
using Spoleto.TrueApi.Auth.Clients;
using Spoleto.TrueApi.Auth.Providers;
using Spoleto.TrueApi.Edo.Exceptions;
using Spoleto.TrueApi.Edo.Extensions;
using Spoleto.TrueApi.Edo.Helpers;
using Spoleto.TrueApi.Edo.Models;
using Spoleto.TrueApi.Edo.Models.Options;
using Spoleto.TrueApi.Edo.Models.Upd;

namespace Spoleto.TrueApi.Edo.Providers
{
    /// <summary>
    /// Провайдер методов «ЭДО Лайт» в True API.
    /// </summary>
    [Obsolete("Используйте TrueApiEdmProvider из пакета Spoleto.Edm.TrueApi")]
    public class TrueApiEdoProvider : ITrueApiEdoProvider, IDisposable
    {
        private readonly ITrueApiTokenProvider _tokenProvider;

        private readonly HttpClient _httpClient;
        private readonly bool _disposeHttpClient;

        public TrueApiEdoProvider() : this(new HttpClient(), true)
        {
            _httpClient.ConfigureHttpClient();
        }

        public TrueApiEdoProvider(HttpClient httpClient, bool disposeHttpClient) : this(httpClient, disposeHttpClient, new TrueApiTokenProvider(httpClient, false))
        {
        }

        public TrueApiEdoProvider(HttpClient httpClient, bool disposeHttpClient, ITrueApiTokenProvider tokenProvider)
        {
            _httpClient = httpClient;
            _disposeHttpClient = disposeHttpClient;
            _tokenProvider = tokenProvider;
        }

        #region IDisposable
        bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (_disposeHttpClient)
                {
                    _httpClient.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        /// <summary>
        /// Клиент на вызов: токен берётся по настройкам вызова. Ошибки и повтор запроса после 401 - в <see cref="TrueApiRestClient"/>.
        /// </summary>
        private TrueApiRestClient CreateRestClient(TrueApiEdoProviderOption settings)
            => new(_httpClient, new TrueApiAuthenticator(_tokenProvider, new(settings.ServiceUrl, settings.CertificateThumbprint, settings.Inn, true)));

        private async Task<string> ExecuteAsStringAsync(TrueApiEdoProviderOption settings, RestRequest request)
        {
            using var restClient = CreateRestClient(settings);
            var response = await restClient.ExecuteAsStringAsync(request).ConfigureAwait(false);

            return response.Content;
        }

        private async Task<BinaryRestResponse> ExecuteAsBytesAsync(TrueApiEdoProviderOption settings, RestRequest request)
        {
            using var restClient = CreateRestClient(settings);

            return await restClient.ExecuteAsBytesAsync(request).ConfigureAwait(false);
        }

        private async Task<T> InvokeAsync<T>(TrueApiEdoProviderOption settings, RestHttpMethod method, Uri uri, HttpContent? content = null)
        {
            var result = await ExecuteAsStringAsync(settings, TrueApiRestClient.CreateRequest(method, uri, content)).ConfigureAwait(false);
            try
            {
                return JsonHelper.FromJson<T>(result)!;
            }
            catch (JsonException ex)
            {
                throw new JsonParsingException(result, ex);
            }
        }

        private async Task<byte[]> InvokeFileAsync(TrueApiEdoProviderOption settings, RestHttpMethod method, Uri uri)
        {
            var response = await ExecuteAsBytesAsync(settings, TrueApiRestClient.CreateRequest(method, uri, isFileResponse: true)).ConfigureAwait(false);

            return response.Content;
        }

        private const string IncomingDocumentsPath = "/api/v3/true-api/elk/incoming-documents";

        private const string OutgoingDocumentsPath = "/api/v3/true-api/elk/outgoing-documents";

        // Подписание исходящих документов и титул покупателя - в разделе edo, а списки и файлы - в elk.
        private const string EdoOutgoingDocumentsPath = "/api/v3/true-api/edo/outgoing-documents";

        private const string EdoIncomingDocumentsPath = "/api/v3/true-api/edo/incoming-documents";

        // Загрузка УПДи - отдельный метод.
        private const string UpdiPath = "/xml/updi";

        // Титул покупателя к УПД и к УПДи - разные методы.
        private const string UpdTitlePath = "/xml/upd/title/970";

        private const string UpdiTitlePath = "/xml/updi/title/970";

        private const string Base64ContentEncoding = "base64";

        private const string XmlMediaType = "text/xml";

        private const string ContentDispositionHeaderName = "Content-Disposition";

        private async Task<EdoDocumentCollection> GetDocumentListAsync(TrueApiEdoProviderOption settings, string documentsPath, EdoDocumentSearchModel? searchModel)
        {
            var path = documentsPath;
            if (searchModel != null)
            {
                var queryString = HttpHelper.ToQueryString(searchModel);
                if (queryString.Length > 0)
                {
                    path += $"?{queryString}";
                }
            }

            var uri = new Uri(UriHelper.UrlCombine(settings.ServiceUrl, path));

            return await InvokeAsync<EdoDocumentCollection>(settings, RestHttpMethod.Get, uri).ConfigureAwait(false);
        }

        private async Task<byte[]> GetDocumentAsync(TrueApiEdoProviderOption settings, string documentsPath, string documentId)
        {
            ArgumentException.ThrowIfNullOrEmpty(documentId);

            var uri = new Uri(UriHelper.UrlCombine(settings.ServiceUrl, $"{documentsPath}/{Uri.EscapeDataString(documentId)}/content"));

            return await InvokeFileAsync(settings, RestHttpMethod.Get, uri).ConfigureAwait(false);
        }

        private async Task<EdoSignedDocumentInfo> GetSignedDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentsPath, string documentId)
        {
            ArgumentException.ThrowIfNullOrEmpty(documentId);

            var uri = new Uri(UriHelper.UrlCombine(settings.ServiceUrl, $"{documentsPath}/{Uri.EscapeDataString(documentId)}"));

            var response = await ExecuteAsBytesAsync(settings, TrueApiRestClient.CreateRequest(RestHttpMethod.Get, uri, isFileResponse: true)).ConfigureAwait(false);

            ContentDispositionHeaderValue? contentDisposition = null;
            if (response.Headers.TryGetValue(ContentDispositionHeaderName, out var values))
            {
                ContentDispositionHeaderValue.TryParse(values.FirstOrDefault(), out contentDisposition);
            }

            var archiveFileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName?.Trim('"');

            using var archive = new ZipArchive(new MemoryStream(response.Content), ZipArchiveMode.Read);

            return EdoArchiveHelper.ReadSignedDocument(archive, archiveFileName);
        }

        private async Task<EdoSellerDocumentInfo> GetSellerDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentsPath, string documentId)
        {
            var document = await GetSignedDocumentInfoAsync(settings, documentsPath, documentId).ConfigureAwait(false);

            return EdoSellerDocumentHelper.ReadSellerDocumentInfo(document);
        }

        /// <summary>
        /// Открепленная подпись файла сертификатом из настроек.
        /// </summary>
        /// <param name="settings">Настройки провайдера: отпечаток сертификата.</param>
        /// <param name="content">Файл документа как есть, байт в байт.</param>
        /// <returns>Подпись CMS в Base64.</returns>
        protected virtual string SignDetached(TrueApiEdoProviderOption settings, byte[] content)
            => CryptographyHelper.SignBase64Data(Convert.ToBase64String(content), detached: true, thumbprint: settings.CertificateThumbprint);

        private static StringContent CreateSignatureContent(string signature)
        {
            var content = new StringContent(signature, Encoding.UTF8, ContentTypes.TextPlain);
            content.Headers.ContentEncoding.Add(Base64ContentEncoding);

            return content;
        }

        /// <summary>
        /// Тело загрузки файла: файл в поле content, для титула покупателя - идентификатор документа продавца в поле doc_id
        /// и, если документ не черновик, подпись в поле signature.
        /// </summary>
        private static MultipartFormDataContent CreateDocumentContent(string fileId, byte[] rawContent, string? signature, string? documentId = null)
        {
            var fileContent = new ByteArrayContent(rawContent);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(XmlMediaType);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "\"content\"", FileName = $"\"{fileId}.xml\"" };

            var content = new MultipartFormDataContent { fileContent };
            if (documentId != null)
            {
                content.Add(new StringContent(documentId, Encoding.UTF8, ContentTypes.TextPlain), "doc_id");
            }

            if (signature != null)
            {
                content.Add(CreateSignatureContent(signature), "signature");
            }

            return content;
        }

        /// <inheritdoc />
        public Task<EdoDocumentCollection> GetIncomingDocumentListAsync(TrueApiEdoProviderOption settings, EdoDocumentSearchModel? searchModel = null)
            => GetDocumentListAsync(settings, IncomingDocumentsPath, searchModel);

        /// <inheritdoc />
        public Task<byte[]> GetIncomingDocumentAsync(TrueApiEdoProviderOption settings, string documentId)
            => GetDocumentAsync(settings, IncomingDocumentsPath, documentId);

        /// <inheritdoc />
        public Task<EdoDocumentCollection> GetOutgoingDocumentListAsync(TrueApiEdoProviderOption settings, EdoDocumentSearchModel? searchModel = null)
            => GetDocumentListAsync(settings, OutgoingDocumentsPath, searchModel);

        /// <inheritdoc />
        public Task<byte[]> GetOutgoingDocumentAsync(TrueApiEdoProviderOption settings, string documentId)
            => GetDocumentAsync(settings, OutgoingDocumentsPath, documentId);

        /// <inheritdoc />
        public Task<EdoSignedDocumentInfo> GetIncomingSignedDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentId)
            => GetSignedDocumentInfoAsync(settings, IncomingDocumentsPath, documentId);

        /// <inheritdoc />
        public Task<EdoSignedDocumentInfo> GetOutgoingSignedDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentId)
            => GetSignedDocumentInfoAsync(settings, OutgoingDocumentsPath, documentId);

        /// <inheritdoc />
        public Task<EdoSellerDocumentInfo> GetIncomingSellerDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentId)
            => GetSellerDocumentInfoAsync(settings, IncomingDocumentsPath, documentId);

        /// <inheritdoc />
        public Task<EdoSellerDocumentInfo> GetOutgoingSellerDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentId)
            => GetSellerDocumentInfoAsync(settings, OutgoingDocumentsPath, documentId);

        /// <inheritdoc />
        public async Task SignOutgoingDocumentAsync(TrueApiEdoProviderOption settings, string documentId)
        {
            // Подписываем байты, полученные от сервиса, без перекодирования: подпись сверяется с файлом черновика.
            var content = await GetOutgoingDocumentAsync(settings, documentId).ConfigureAwait(false);
            var signature = SignDetached(settings, content);

            var uri = new Uri(UriHelper.UrlCombine(settings.ServiceUrl, $"{EdoOutgoingDocumentsPath}/{Uri.EscapeDataString(documentId)}/signature"));

            await ExecuteAsStringAsync(settings, TrueApiRestClient.CreateRequest(RestHttpMethod.Post, uri, CreateSignatureContent(signature))).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EdoPostDocumentResult> PostUniversalTransferDocumentAsync(TrueApiEdoProviderOption settings, EdoSellerUniversalTransferDocument document, bool isDraft = false)
        {
            ArgumentNullException.ThrowIfNull(document);

            var fileId = string.IsNullOrEmpty(document.FileId) ? EdoUniversalTransferDocumentXmlHelper.GetFileId(document) : document.FileId;
            var content = EdoUniversalTransferDocumentXmlHelper.CreateXml(document, fileId);
            var rawContent = EdoUniversalTransferDocumentXmlHelper.FileEncoding.GetBytes(content);

            string? id;
            try
            {
                // Без подписи документ остаётся черновиком, с подписью сразу уходит получателю.
                var signature = isDraft ? null : SignDetached(settings, rawContent);

                var path = document.IsUPDi ? EdoOutgoingDocumentsPath + UpdiPath : EdoOutgoingDocumentsPath;
                var uri = new Uri(UriHelper.UrlCombine(settings.ServiceUrl, path));

                var result = await InvokeAsync<EdoIdResult>(settings, RestHttpMethod.Post, uri, CreateDocumentContent(fileId, rawContent, signature)).ConfigureAwait(false);
                id = result?.Id;
                if (string.IsNullOrEmpty(id))
                {
                    throw new InvalidDataException($"«ЭДО Лайт» не вернул идентификатор загруженного файла {fileId}.");
                }
            }
            catch (Exception ex)
            {
                throw new EdoPostDocumentException(fileId, content, rawContent, ex);
            }

            return new EdoPostDocumentResult
            {
                Id = id,
                FileId = fileId,
                Content = content,
                RawContent = rawContent
            };
        }

        /// <inheritdoc />
        public async Task<EdoPostDocumentResult> ReceiptUniversalTransferDocumentAsync(TrueApiEdoProviderOption settings, EdoBuyerUniversalTransferDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentException.ThrowIfNullOrEmpty(document.DocumentId);

            // Информация покупателя повторяет реквизиты и подпись файла продавца, а её имя - участников и признаки из имени файла продавца.
            var sellerDocumentInfo = await GetIncomingSellerDocumentInfoAsync(settings, document.DocumentId).ConfigureAwait(false);

            var fileId = string.IsNullOrEmpty(document.FileId) ? EdoUniversalTransferDocumentXmlHelper.GetFileId(document, sellerDocumentInfo) : document.FileId;
            var content = EdoUniversalTransferDocumentXmlHelper.CreateXml(document, sellerDocumentInfo, fileId);
            var rawContent = EdoUniversalTransferDocumentXmlHelper.FileEncoding.GetBytes(content);

            string? id;
            try
            {
                // Титул покупателя загружается только с подписью: черновиков у него нет.
                var signature = SignDetached(settings, rawContent);

                var path = EdoIncomingDocumentsPath + (sellerDocumentInfo.IsUPDi ? UpdiTitlePath : UpdTitlePath);
                var uri = new Uri(UriHelper.UrlCombine(settings.ServiceUrl, path));

                var result = await InvokeAsync<EdoIdResult>(settings, RestHttpMethod.Post, uri, CreateDocumentContent(fileId, rawContent, signature, document.DocumentId)).ConfigureAwait(false);

                // Описание метода 9.2.9 обещает event_id, а тестовый стенд 29.10.2026 вернул только id.
                id = result?.EventId ?? result?.Id;
                if (string.IsNullOrEmpty(id))
                {
                    throw new InvalidDataException($"«ЭДО Лайт» не вернул идентификатор загруженного титула покупателя {fileId}.");
                }
            }
            catch (Exception ex)
            {
                throw new EdoPostDocumentException(fileId, content, rawContent, ex);
            }

            return new EdoPostDocumentResult
            {
                Id = id,
                FileId = fileId,
                Content = content,
                RawContent = rawContent
            };
        }
    }
}
