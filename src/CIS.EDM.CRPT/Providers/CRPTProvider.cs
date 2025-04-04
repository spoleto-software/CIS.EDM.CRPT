﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CIS.EDM.CRPT.Helpers;
using CIS.EDM.CRPT.Models;
using CIS.EDM.Extensions;
using CIS.EDM.Helpers;
using CIS.EDM.Models;
using Microsoft.Extensions.Logging;

namespace CIS.EDM.CRPT.Providers
{
    /// <summary>
    /// Провайдер для работы с ЭДО от ЦРПТ.
    /// </summary>
    public class CRPTProvider : ICRPTProvider
    {
        private readonly ILogger<CRPTProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICRPTTokenProvider _tokenProvider;

        /// <summary>
        /// Токен доступа к сервису ЦРПТ.
        /// </summary>
        private TokenModel _token;

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        public CRPTProvider(ILogger<CRPTProvider> logger, IHttpClientFactory httpClientFactory, ICRPTTokenProvider tokenProvider = null) : base()
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _tokenProvider = tokenProvider ?? new CRPTTokenProvider(httpClientFactory);
        }

        /// <summary>
        /// Уникальный код провайдера.
        /// </summary>
        public string Code => "CRPT";

        /// <summary>
        /// Название провайдера для отображения
        /// </summary>
        public string DisplayName => "ЦРПТ";

        /// <summary>
        /// Текстовое представление провайдера.
        /// </summary>
        public override string ToString() => DisplayName;

        private async Task InitHeadersAsync(HttpRequestMessage requestMessage, CRPTOption settings, bool isZipResponse = false)
        {
            requestMessage.ConfigureRequestMessage(isZipResponse);

            var token = await GetTokenAsync(settings).ConfigureAwait(false);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(token.Type, token.Token);
        }

        private async Task<TokenModel> GetTokenAsync(CRPTOption settings)
        {
            if (_token == null)
                _token = await _tokenProvider.GetTokenAsync(settings).ConfigureAwait(false);

            return _token;
        }

        private async Task<T> InvokeAsync<T>(CRPTOption settings, Uri uri, HttpMethod method, HttpContent content = null, bool isZipResponse = false, bool canToResetToken = true)
        {
            using var client = _httpClientFactory.CreateClient();
            client.ConfigureHttpClient();

            using var requestMessage = new HttpRequestMessage(method, uri);
            await InitHeadersAsync(requestMessage, settings, isZipResponse).ConfigureAwait(false);

            if (content != null)
            {
                requestMessage.Content = content;
            }

            using var responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);

            if (responseMessage.IsSuccessStatusCode)
            {
                if (isZipResponse)
                {
                    var contentDisposition = responseMessage.Content.Headers.ContentDisposition;
                    string fileName = null;
                    if (contentDisposition != null)
                    {
                        fileName = contentDisposition.FileName?.Trim('"');
                    }

                    var memoryStream = new MemoryStream();
                    await responseMessage.Content.CopyToAsync(memoryStream).ConfigureAwait(false);
                    memoryStream.Position = 0;

                    var archiveInfo = new ZipArchiveInfo
                    {
                        ZipArchive = new ZipArchive(memoryStream),
                        FileName = fileName
                    };

                    return (T)(object)archiveInfo;
                }

                if (responseMessage.Content.Headers.ContentType?.MediaType == ContentType.OctetStream
                    && typeof(T) == typeof(string))
                {
                    var bytes = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    var str = XmlHelper.DefaultEncoding.GetString(bytes);
                    return (T)(object)str;
                }

                var result = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (String.IsNullOrEmpty(result))
                    return default;

                try
                {
                    var objectResult = HttpHelper.FromJson<T>(result);
                    return objectResult;
                }
                catch (JsonException ex)
                {
                    var json = result;
                    var message = $"Ошибка десериализации ответа от сервиса. Исходный JSON:{Environment.NewLine}{json}";

                    throw new JsonException(message, ex);
                }
            }

            var errorResult = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!String.IsNullOrEmpty(errorResult))
            {
                if (responseMessage.Content.Headers.ContentType?.MediaType == ContentType.ApplicationJson)
                {
                    try
                    {
                        var errorModel = HttpHelper.FromJson<ErrorModel>(errorResult);

                        throw new Exception(errorModel.ToString());
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        throw new Exception(errorResult);
                    }
                }
                else
                {
                    throw new Exception(errorResult);
                }
            }
            else if (responseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized
                && canToResetToken)
            {
                // Кейс с истекшим токеном. Сбросим текущий токен только один раз. Так как если это не помогло, то, возможно, дело в другом.
                _token = null;
                return await InvokeAsync<T>(settings, uri, method, content, canToResetToken: false).ConfigureAwait(false);
            }
            else
            {
                throw new Exception(responseMessage.ReasonPhrase);
            }
        }

        /// <summary>
        /// Получение списка входящих документов
        /// </summary>
        /// <param name="settings">Настройки для API</param>
        /// <param name="searchModel">Критерии отбора документов</param>
        /// <returns>Список входящих документов</returns>
        public async Task<DocumentCollection> GetIncomingDocumentListAsync(CRPTOption settings, DocumentCollectionSearchModel searchModel = null)
        {
            var sUrl = $"/api/v1/incoming-documents";
            if (searchModel != null)
            {
                var queryString = HttpHelper.ToQueryString(searchModel);
                sUrl += $"?{queryString}";
            }
            var uri = new Uri(new Uri(settings.ServiceUrl), sUrl);

            return await InvokeAsync<DocumentCollection>(settings, uri, HttpMethod.Get).ConfigureAwait(false);
        }

        /// <summary>
        /// Получение списка исходящих документов
        /// </summary>
        /// <param name="settings">Настройки для API</param>
        /// <param name="searchModel">Критерии отбора документов</param>
        /// <returns>Список исходящих документов</returns>
        public async Task<DocumentCollection> GetOutgoingDocumentListAsync(CRPTOption settings, DocumentCollectionSearchModel searchModel = null)
        {
            var sUrl = $"/api/v1/outgoing-documents";
            if (searchModel != null)
            {
                var queryString = HttpHelper.ToQueryString(searchModel);
                sUrl += $"?{queryString}";
            }

            var uri = new Uri(new Uri(settings.ServiceUrl), sUrl);

            return await InvokeAsync<DocumentCollection>(settings, uri, HttpMethod.Get).ConfigureAwait(false);
        }

        /// <summary>
        /// Получение содержимого XML входящего документа
        /// </summary>
        /// <param name="settings">Настройки для API</param>
        /// <param name="documentId">Идентификатор документа</param>
        /// <returns>Содержимое XML входящего документа</returns>
        public async Task<string> GetIncomingDocumentAsync(CRPTOption settings, string documentId)
        {
            var sUrl = $"/api/v1/incoming-documents/{documentId}/content";
            var uri = new Uri(new Uri(settings.ServiceUrl), sUrl);

            return await InvokeAsync<string>(settings, uri, HttpMethod.Get).ConfigureAwait(false);
        }

        /// <summary>
        /// Получение содержимого XML исходящего документа
        /// </summary>
        /// <param name="settings">Настройки для API</param>
        /// <param name="documentId">Идентификатор документа</param>
        /// <returns>Содержимое XML исходящего документа</returns>
        public async Task<string> GetOutgoingDocumentAsync(CRPTOption settings, string documentId)
        {
            var sUrl = $"/api/v1/outgoing-documents/{documentId}/content";
            var uri = new Uri(new Uri(settings.ServiceUrl), sUrl);

            return await InvokeAsync<string>(settings, uri, HttpMethod.Get).ConfigureAwait(false);
        }

        /// <summary>
        /// Подписание исходящего документа
        /// </summary>
        /// <param name="settings">Настройки для API</param>
        /// <param name="documentId">Идентификатор документа</param>
        public async Task SignOutgoingDocumentAsync(CRPTOption settings, string documentId)
        {
            var sUrl = $"/api/v1/outgoing-documents/{documentId}/signature";
            var uri = new Uri(new Uri(settings.ServiceUrl), sUrl);

            var xmlBody = await GetOutgoingDocumentAsync(settings, documentId).ConfigureAwait(false);

            var contentIn64 = HttpHelper.ConvertToBase64(xmlBody, XmlHelper.DefaultEncoding);
            var signature = Cryptography.CryptographyHelper.SignBase64Data(contentIn64, detached: true, thumbprint: settings.CertificateThumbprint);
            var signatureContent = new StringContent(signature, Encoding.UTF8, ContentType.TextPlain);
            signatureContent.Headers.ContentEncoding.Add("base64");

            await InvokeAsync<object>(settings, uri, HttpMethod.Post, signatureContent).ConfigureAwait(false);
        }

        /// <summary>
        /// Отправка универсального передаточного документа (УПД) в формате приказа "№820".
        /// Метод загрузки файла информации продавца УПД согласно приказа 820 от 19.12.2018 № ММВ-7-15/820@ в формате XML
        /// </summary>
        /// <param name="settings">Настройки для API.</param>
        /// <param name="sellerDataContract">Информация продавца.</param>
        /// <param name="isDraft">Создать только черновник. Не отправлять документ получателю.</param>
        /// <returns>Идентификатор сообщения.</returns>
        public async Task<ResultInfo> PostUniversalTransferDocumentAsync(CRPTOption settings, EDM.Models.V5_01.Seller.SellerUniversalTransferDocument sellerDataContract, bool isDraft = false)
        {
            var address = sellerDataContract.IsHyphenRevisionNumber ? "/api/v1/outgoing-documents" : "/api/v1/outgoing-documents/xml/updi";
            var uri = new Uri(new Uri(settings.ServiceUrl), address);

            var xmlDoc = XmlHelper.GenerateXml(sellerDataContract);

            var result = await PostUniversalTransferDocumentAsync(settings, uri, xmlDoc, isDraft).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Отправка универсального передаточного документа (УПД) в формате приказа "№970".
        /// Метод загрузки файла информации продавца УПД согласно приказа ФНС России от 19.12.2023 N ЕД-7-26/970@ (ред. от 15.11.2024) в формате XML
        /// </summary>
        /// <param name="settings">Настройки для API.</param>
        /// <param name="sellerDataContract">Информация продавца.</param>
        /// <param name="isDraft">Создать только черновник. Не отправлять документ получателю.</param>
        /// <returns>Идентификатор сообщения.</returns>
        public async Task<ResultInfo> PostUniversalTransferDocumentAsync(CRPTOption settings, CIS.EDM.Models.V5_03.Seller.SellerUniversalTransferDocument sellerDataContract, bool isDraft = false)
        {
            var address = string.IsNullOrEmpty(sellerDataContract.RevisionNumber) ? "/api/v1/outgoing-documents" : "/api/v1/outgoing-documents/xml/updi";
            var uri = new Uri(new Uri(settings.ServiceUrl), address);

            var xmlDoc = XmlHelperV5_03.GenerateXml(sellerDataContract);

            var result = await PostUniversalTransferDocumentAsync(settings, uri, xmlDoc, isDraft).ConfigureAwait(false);

            return result;
        }

        private async Task<ResultInfo> PostUniversalTransferDocumentAsync(CRPTOption settings, Uri uri, DocumentData selllerDocumentData, bool isDraft = false)
        {
            var xmlDoc = selllerDocumentData;

            // convert string to stream
            var byteArray = xmlDoc.FileEncoding.GetBytes(xmlDoc.Content);
            using var stream = new MemoryStream(byteArray);

            var streamContent = new StreamContent(stream);
            streamContent.Headers.Add("Content-Type", ContentType.TextXml);
            streamContent.Headers.Add("Content-Disposition", "form-data; name=\"content\"; filename=\"" + xmlDoc.Id + "\"");

            var content = new MultipartFormDataContent
            {
                { streamContent }
            };

            if (!isDraft)
            {
                var contentIn64 = HttpHelper.ConvertToBase64(xmlDoc.Content, xmlDoc.FileEncoding);
                var signature = Cryptography.CryptographyHelper.SignBase64Data(contentIn64, detached: true, thumbprint: settings.CertificateThumbprint);
                var signatureContent = new StringContent(signature, Encoding.UTF8, ContentType.TextPlain);
                signatureContent.Headers.ContentEncoding.Add("base64");

                content.Add(signatureContent, "signature");
            }

            var resultInfo = new ResultInfo
            {
                Content = xmlDoc.Content
            };

            try
            {
                var result = await InvokeAsync<StringResult>(settings, uri, HttpMethod.Post, content, canToResetToken: true).ConfigureAwait(false);
                resultInfo.Id = result?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправки универсального передаточного документа.");
                resultInfo.Exception = ex;
            }

            return resultInfo;
        }

        /// <summary>
        /// Добавление извещения о получении к документу в формате приказа "№820".
        /// </summary>
        /// <param name="settings">Настройки для API.</param>
        /// <param name="buyerDataContract">Информация покупателя.</param>
        /// <returns>Идентификатор созданного извещения о получении</returns>
        public async Task<ResultInfo> ReceiptUniversalTransferDocumentAsync(CRPTOption settings, EDM.Models.V5_01.Buyer.BuyerUniversalTransferDocument buyerDataContract)
        {
            var sellerDocument = await GetIncomingSignedDocumentInfoAsync(settings, buyerDataContract.EdmDocumentId).ConfigureAwait(false);
            buyerDataContract.SellerDocumentInfo = XmlParser.SellerInfoFromXml(sellerDocument);

            var xmlDoc = XmlHelper.GenerateXml(buyerDataContract);

            var address = buyerDataContract.SellerDocumentInfo.IsUPDi ? "/api/v1/incoming-documents/xml/updi/title" : "/api/v1/incoming-documents/xml/upd/title";
            var uri = new Uri(new Uri(settings.ServiceUrl), address);

            var result = await ReceiptUniversalTransferDocumentAsync(settings, uri, buyerDataContract.EdmDocumentId, xmlDoc).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Добавление извещения о получении к документу в формате приказа "№970".
        /// </summary>
        /// <param name="settings">Настройки для API.</param>
        /// <param name="buyerDataContract">Информация покупателя.</param>
        /// <returns>Идентификатор созданного извещения о получении</returns>
        public async Task<ResultInfo> ReceiptUniversalTransferDocumentAsync(CRPTOption settings, EDM.Models.V5_03.Buyer.BuyerUniversalTransferDocument buyerDataContract)
        {
            var sellerDocument = await GetIncomingSignedDocumentInfoAsync(settings, buyerDataContract.EdmDocumentId).ConfigureAwait(false);
            buyerDataContract.SellerDocumentInfo = XmlParserV5_03.SellerInfoFromXml(sellerDocument);

            var xmlDoc = XmlHelperV5_03.GenerateXml(buyerDataContract);

            var address = buyerDataContract.SellerDocumentInfo.IsUPDi ? "/api/v1/incoming-documents/xml/updi/title/970" : "/api/v1/incoming-documents/xml/upd/title/970";
            var uri = new Uri(new Uri(settings.ServiceUrl), address);

            var result = await ReceiptUniversalTransferDocumentAsync(settings, uri, buyerDataContract.EdmDocumentId, xmlDoc).ConfigureAwait(false);

            return result;
        }

        private async Task<ResultInfo> ReceiptUniversalTransferDocumentAsync(CRPTOption settings, Uri uri, string sellerDocumentId, DocumentData buyerDocumentData)
        {
            var xmlDoc = buyerDocumentData;

            // convert string to stream
            var byteArray = xmlDoc.FileEncoding.GetBytes(xmlDoc.Content);
            using var stream = new MemoryStream(byteArray);

            var streamContent = new StreamContent(stream);
            streamContent.Headers.Add("Content-Type", ContentType.TextXml);
            streamContent.Headers.Add("Content-Disposition", "form-data; name=\"content\"; filename=\"" + xmlDoc.Id + "\"");

            var docIdContent = new StringContent(sellerDocumentId, Encoding.UTF8, ContentType.TextPlain);

            var contentIn64 = HttpHelper.ConvertToBase64(xmlDoc.Content, xmlDoc.FileEncoding);
            var signature = Cryptography.CryptographyHelper.SignBase64Data(contentIn64, detached: true, thumbprint: settings.CertificateThumbprint);
            var signatureContent = new StringContent(signature, Encoding.UTF8, ContentType.TextPlain);
            signatureContent.Headers.ContentEncoding.Add("base64");

            var content = new MultipartFormDataContent
            {
                { streamContent },
                { docIdContent, "doc_id" },
                { signatureContent, "signature" }

            };

            var resultInfo = new ResultInfo
            {
                Content = xmlDoc.Content
            };

            try
            {
                var result = await InvokeAsync<StringResult>(settings, uri, HttpMethod.Post, content).ConfigureAwait(false);
                resultInfo.Id = result?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении извещения о получении к документу.");
                resultInfo.Exception = ex;
            }

            return resultInfo;
        }

        public async Task<SignedDocumentInfo> GetIncomingSignedDocumentInfoAsync(CRPTOption settings, string documentId)
        {
            var sUrl = $"/api/v1/incoming-documents/{documentId}";
            var uri = new Uri(new Uri(settings.ServiceUrl), sUrl);

            var zipArchiveInfo = await InvokeAsync<ZipArchiveInfo>(settings, uri, HttpMethod.Get, isZipResponse: true).ConfigureAwait(false);
            using var zipArchive = zipArchiveInfo.ZipArchive;
            ZipArchiveEntry contentFileEntry;
            ZipArchiveEntry signatureFileEntry;
            if (!String.IsNullOrEmpty(zipArchiveInfo.FileName))
            {
                var fileName = zipArchiveInfo.FileName.Replace(".zip", string.Empty);
                contentFileEntry = zipArchive.GetEntry(fileName + ".xml");
                signatureFileEntry = zipArchive.GetEntry(fileName + ".p7s");
            }
            else
            {
                var fileEntries = zipArchive.Entries.Where(x => x.Name.StartsWith(EDM.Models.V5_03.Seller.SellerUniversalTransferDocument.FileIdPattern));
                contentFileEntry = fileEntries.Single(x => x.Name.EndsWith(".xml", StringComparison.Ordinal));
                signatureFileEntry = fileEntries.Single(x => x.Name.EndsWith(".p7s", StringComparison.Ordinal));
            }

            using var contentFileEntryStream = contentFileEntry.Open();
            using var contentReader = new StreamReader(contentFileEntryStream, XmlHelper.DefaultEncoding);
            var content = contentReader.ReadToEnd();

            using var signatureFileEntryStream = signatureFileEntry.Open();
            using var signatureReader = new MemoryStream();
            signatureFileEntryStream.CopyTo(signatureReader);
            var signatureBytes = signatureReader.ToArray();
            var signature = Convert.ToBase64String(signatureBytes);

            var sellerDocument = new SignedDocumentInfo
            {
                Content = content,
                DetachedSignature = signature
            };

            return sellerDocument;
        }
    }
}
