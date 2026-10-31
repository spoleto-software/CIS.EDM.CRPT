using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CIS.EDM.CRPT.Helpers;
using CIS.EDM.CRPT.Models;
using CIS.EDM.Extensions;
using CIS.EDM.Helpers;
using CIS.EDM.Models;
using Microsoft.Extensions.Logging;
using Spoleto.TrueApi.Auth.Providers;
using Spoleto.TrueApi.Edo.Models;
using Spoleto.TrueApi.Edo.Models.Options;
using Spoleto.TrueApi.Edo.Providers;

namespace CIS.EDM.CRPT.Providers
{
    /// <summary>
    /// Провайдер для работы с ЭДО от ЦРПТ через методы «ЭДО Лайт» в True API (раздел 9).
    /// </summary>
    /// <remarks>
    /// Обёртка над <see cref="TrueApiEdoProvider"/> из Spoleto.TrueApi.Edo: модели CIS.EDM переводятся в модели «ЭДО Лайт» и обратно.
    /// УПД переносятся только в формате 970 (модели V5_03), методы формата 820 (модели V5_01) бросают <see cref="NotSupportedException"/>.
    /// <para>
    /// Адрес True API - <see cref="CRPTOption.AuthUrl"/>: по нему и методы «ЭДО Лайт», и токен.
    /// <see cref="CRPTOption.ServiceUrl"/> - адрес прежнего API «ЭДО Лайт», этому провайдеру он не нужен.
    /// </para>
    /// <para>
    /// Провайдер запрашивает UUID-токен, а <see cref="CRPTProvider"/> - JWT, а провайдер токенов хранит один токен:
    /// не передавайте обоим провайдерам один экземпляр <see cref="ITrueApiTokenProvider"/>.
    /// </para>
    /// </remarks>
    public class TrueApiCRPTProvider : ICRPTProvider
    {
        private const string Format820NotSupportedMessage = "УПД формата 820 (модели V5_01) в True API не перенесены: используйте модели V5_03 (формат 970).";

        private readonly ILogger<TrueApiCRPTProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITrueApiTokenProvider _tokenProvider;

        static TrueApiCRPTProvider()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//support for "windows-1251"
        }

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        public TrueApiCRPTProvider(ILogger<TrueApiCRPTProvider> logger, IHttpClientFactory httpClientFactory, ITrueApiTokenProvider tokenProvider = null)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _tokenProvider = tokenProvider ?? new TrueApiTokenProvider();
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

        /// <summary>
        /// Провайдер «ЭДО Лайт» на один вызов.
        /// </summary>
        /// <param name="httpClient">Клиент из <see cref="IHttpClientFactory"/>, освобождается после вызова.</param>
        protected virtual TrueApiEdoProvider CreateEdoProvider(HttpClient httpClient) => new(httpClient, false, _tokenProvider);

        private static TrueApiEdoProviderOption CreateEdoOption(CRPTOption settings)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentException.ThrowIfNullOrEmpty(settings.AuthUrl, nameof(settings.AuthUrl));

            return new()
            {
                ServiceUrl = settings.AuthUrl,
                CertificateThumbprint = settings.CertificateThumbprint,
                Inn = settings.Inn
            };
        }

        private async Task<T> InvokeAsync<T>(CRPTOption settings, Func<TrueApiEdoProvider, TrueApiEdoProviderOption, Task<T>> invoke)
        {
            var edoSettings = CreateEdoOption(settings);

            using var client = _httpClientFactory.CreateClient();
            client.ConfigureHttpClient();

            using var edoProvider = CreateEdoProvider(client);

            return await invoke(edoProvider, edoSettings).ConfigureAwait(false);
        }

        private Task<DocumentCollection> GetDocumentListAsync(CRPTOption settings, DocumentCollectionSearchModel searchModel,
            Func<TrueApiEdoProvider, TrueApiEdoProviderOption, EdoDocumentSearchModel, Task<EdoDocumentCollection>> getList)
        {
            // Критерии переводим до запроса: неподдерживаемая сортировка не должна стоить токена.
            var searchModels = TrueApiEdoMapper.MapSearchModels(searchModel);

            return InvokeAsync(settings, async (edoProvider, edoSettings) =>
            {
                var collections = new List<EdoDocumentCollection>();
                foreach (var edoSearchModel in searchModels)
                {
                    collections.Add(await getList(edoProvider, edoSettings, edoSearchModel).ConfigureAwait(false));
                }

                return TrueApiEdoMapper.Map(collections, searchModel?.Asc == true);
            });
        }

        /// <summary>
        /// Файл документа строкой: XML - в кодировке из своего заголовка (УПД - windows-1251), остальное (PDF договоров) - в windows-1251, как в <see cref="CRPTProvider"/>.
        /// </summary>
        private static string ReadDocument(byte[] content)
        {
            Encoding encoding;
            try
            {
                using var xmlReader = new XmlTextReader(new MemoryStream(content)) { DtdProcessing = DtdProcessing.Prohibit };
                xmlReader.MoveToContent();
                encoding = xmlReader.Encoding ?? XmlHelper.DefaultEncoding;
            }
            catch (XmlException)
            {
                encoding = XmlHelper.DefaultEncoding;
            }

            using var reader = new StreamReader(new MemoryStream(content), encoding, true);

            return reader.ReadToEnd();
        }

        /// <summary>
        /// Загрузка файла информации продавца или покупателя. Как и в <see cref="CRPTProvider"/>, ошибка не бросается, а возвращается в <see cref="ResultInfo.Exception"/>.
        /// </summary>
        private async Task<ResultInfo> PostDocumentAsync(CRPTOption settings, Func<TrueApiEdoProvider, TrueApiEdoProviderOption, Task<EdoPostDocumentResult>> post, string errorMessage)
        {
            try
            {
                var result = await InvokeAsync(settings, post).ConfigureAwait(false);

                return new ResultInfo
                {
                    Id = result.Id,
                    Content = result.Content
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, errorMessage);

                return new ResultInfo { Exception = ex };
            }
        }

        /// <inheritdoc />
        public Task<DocumentCollection> GetIncomingDocumentListAsync(CRPTOption settings, DocumentCollectionSearchModel searchModel = null)
            => GetDocumentListAsync(settings, searchModel, (edoProvider, edoSettings, edoSearchModel) => edoProvider.GetIncomingDocumentListAsync(edoSettings, edoSearchModel));

        /// <inheritdoc />
        public Task<DocumentCollection> GetOutgoingDocumentListAsync(CRPTOption settings, DocumentCollectionSearchModel searchModel = null)
            => GetDocumentListAsync(settings, searchModel, (edoProvider, edoSettings, edoSearchModel) => edoProvider.GetOutgoingDocumentListAsync(edoSettings, edoSearchModel));

        /// <inheritdoc />
        public Task<string> GetIncomingDocumentAsync(CRPTOption settings, string documentId)
            => InvokeAsync(settings, async (edoProvider, edoSettings) => ReadDocument(await edoProvider.GetIncomingDocumentAsync(edoSettings, documentId).ConfigureAwait(false)));

        /// <inheritdoc />
        public Task<string> GetOutgoingDocumentAsync(CRPTOption settings, string documentId)
            => InvokeAsync(settings, async (edoProvider, edoSettings) => ReadDocument(await edoProvider.GetOutgoingDocumentAsync(edoSettings, documentId).ConfigureAwait(false)));

        /// <inheritdoc />
        public Task SignOutgoingDocumentAsync(CRPTOption settings, string documentId)
            => InvokeAsync<object>(settings, async (edoProvider, edoSettings) =>
            {
                await edoProvider.SignOutgoingDocumentAsync(edoSettings, documentId).ConfigureAwait(false);
                return null;
            });

        /// <inheritdoc />
        public Task<ResultInfo> PostUniversalTransferDocumentAsync(CRPTOption settings, EDM.Models.V5_01.Seller.SellerUniversalTransferDocument sellerDataContract, bool isDraft = false)
            => throw new NotSupportedException(Format820NotSupportedMessage);

        /// <inheritdoc />
        public Task<ResultInfo> PostUniversalTransferDocumentAsync(CRPTOption settings, EDM.Models.V5_03.Seller.SellerUniversalTransferDocument sellerDataContract, bool isDraft = false)
        {
            var document = TrueApiEdoMapper.Map(sellerDataContract);

            return PostDocumentAsync(settings, (edoProvider, edoSettings) => edoProvider.PostUniversalTransferDocumentAsync(edoSettings, document, isDraft),
                "Ошибка при отправке универсального передаточного документа.");
        }

        /// <inheritdoc />
        public Task<ResultInfo> ReceiptUniversalTransferDocumentAsync(CRPTOption settings, EDM.Models.V5_01.Buyer.BuyerUniversalTransferDocument buyerDataContract)
            => throw new NotSupportedException(Format820NotSupportedMessage);

        /// <inheritdoc />
        /// <remarks>
        /// Сведения о файле продавца, отправителя и получателя провайдер берёт из входящего документа <c>EdmDocumentId</c>,
        /// <c>SellerDocumentInfo</c> модели не заполняется. Ответ «ЭДО Лайт» - в <see cref="ResultInfo.Id"/>, сам документ остаётся под прежним идентификатором.
        /// </remarks>
        public Task<ResultInfo> ReceiptUniversalTransferDocumentAsync(CRPTOption settings, EDM.Models.V5_03.Buyer.BuyerUniversalTransferDocument buyerDataContract)
        {
            var document = TrueApiEdoMapper.Map(buyerDataContract);

            return PostDocumentAsync(settings, (edoProvider, edoSettings) => edoProvider.ReceiptUniversalTransferDocumentAsync(edoSettings, document),
                "Ошибка при добавлении извещения о получении к документу.");
        }

        /// <inheritdoc />
        public Task<SignedDocumentInfo> GetIncomingSignedDocumentInfoAsync(CRPTOption settings, string documentId)
            => InvokeAsync(settings, async (edoProvider, edoSettings) => TrueApiEdoMapper.Map(await edoProvider.GetIncomingSignedDocumentInfoAsync(edoSettings, documentId).ConfigureAwait(false)));

        /// <summary>
        /// Получение информации об исходящем документе (его тела и открепленной подписи).
        /// </summary>
        /// <param name="settings">Настройки для API</param>
        /// <param name="documentId">Идентификатор документа</param>
        public Task<SignedDocumentInfo> GetOutgoingSignedDocumentInfoAsync(CRPTOption settings, string documentId)
            => InvokeAsync(settings, async (edoProvider, edoSettings) => TrueApiEdoMapper.Map(await edoProvider.GetOutgoingSignedDocumentInfoAsync(edoSettings, documentId).ConfigureAwait(false)));

        /// <inheritdoc />
        public Task<SellerDocumentInfo> GetIncomingSellerDocumentInfoAsync(CRPTOption settings, string sellerDocumentId)
            => InvokeAsync(settings, async (edoProvider, edoSettings) => TrueApiEdoMapper.Map(await edoProvider.GetIncomingSellerDocumentInfoAsync(edoSettings, sellerDocumentId).ConfigureAwait(false)));

        /// <inheritdoc />
        public Task<SellerDocumentInfo> GetOutgoingSellerDocumentInfoAsync(CRPTOption settings, string sellerDocumentId)
            => InvokeAsync(settings, async (edoProvider, edoSettings) => TrueApiEdoMapper.Map(await edoProvider.GetOutgoingSellerDocumentInfoAsync(edoSettings, sellerDocumentId).ConfigureAwait(false)));
    }
}
