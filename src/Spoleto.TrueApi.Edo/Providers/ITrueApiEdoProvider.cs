using Spoleto.TrueApi.Edo.Exceptions;
using Spoleto.TrueApi.Edo.Models;
using Spoleto.TrueApi.Edo.Models.Options;
using Spoleto.TrueApi.Edo.Models.Upd;

namespace Spoleto.TrueApi.Edo.Providers
{
    /// <summary>
    /// Провайдер методов «ЭДО Лайт» в True API.
    /// </summary>
    [Obsolete("Используйте ITrueApiEdmProvider из пакета Spoleto.Edm.TrueApi")]
    public interface ITrueApiEdoProvider
    {
        /// <summary>
        /// Получение списка входящих документов.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="searchModel">Критерии отбора документов.</param>
        /// <returns>Страница списка входящих документов.</returns>
        EdoDocumentCollection GetIncomingDocumentList(TrueApiEdoProviderOption settings, EdoDocumentSearchModel? searchModel = null)
            => GetIncomingDocumentListAsync(settings, searchModel).GetAwaiter().GetResult();

        /// <summary>
        /// Получение списка входящих документов.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="searchModel">Критерии отбора документов.</param>
        /// <returns>Страница списка входящих документов.</returns>
        Task<EdoDocumentCollection> GetIncomingDocumentListAsync(TrueApiEdoProviderOption settings, EdoDocumentSearchModel? searchModel = null);

        /// <summary>
        /// Получение файла входящего документа.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>Файл как есть: XML для УПД, УКД и квитанций (в кодировке файла, для УПД - windows-1251), PDF для договоров с оператором ГИС МТ.</returns>
        byte[] GetIncomingDocument(TrueApiEdoProviderOption settings, string documentId)
            => GetIncomingDocumentAsync(settings, documentId).GetAwaiter().GetResult();

        /// <summary>
        /// Получение файла входящего документа.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>Файл как есть: XML для УПД, УКД и квитанций (в кодировке файла, для УПД - windows-1251), PDF для договоров с оператором ГИС МТ.</returns>
        Task<byte[]> GetIncomingDocumentAsync(TrueApiEdoProviderOption settings, string documentId);

        /// <summary>
        /// Получение списка исходящих документов.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="searchModel">Критерии отбора документов.</param>
        /// <returns>Страница списка исходящих документов.</returns>
        EdoDocumentCollection GetOutgoingDocumentList(TrueApiEdoProviderOption settings, EdoDocumentSearchModel? searchModel = null)
            => GetOutgoingDocumentListAsync(settings, searchModel).GetAwaiter().GetResult();

        /// <summary>
        /// Получение списка исходящих документов.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="searchModel">Критерии отбора документов.</param>
        /// <returns>Страница списка исходящих документов.</returns>
        Task<EdoDocumentCollection> GetOutgoingDocumentListAsync(TrueApiEdoProviderOption settings, EdoDocumentSearchModel? searchModel = null);

        /// <summary>
        /// Получение файла исходящего документа.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>Файл как есть: XML для УПД, УКД и квитанций (в кодировке файла, для УПД - windows-1251), PDF для договоров с оператором ГИС МТ.</returns>
        byte[] GetOutgoingDocument(TrueApiEdoProviderOption settings, string documentId)
            => GetOutgoingDocumentAsync(settings, documentId).GetAwaiter().GetResult();

        /// <summary>
        /// Получение файла исходящего документа.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>Файл как есть: XML для УПД, УКД и квитанций (в кодировке файла, для УПД - windows-1251), PDF для договоров с оператором ГИС МТ.</returns>
        Task<byte[]> GetOutgoingDocumentAsync(TrueApiEdoProviderOption settings, string documentId);

        /// <summary>
        /// Получение файла входящего документа с его открепленной подписью.
        /// </summary>
        /// <remarks>
        /// Скачивает ZIP-архив документа с подписями и квитанциями. Из архива берётся файл, по которому назван архив, а без имени архива - информация продавца УПД / УПДи.
        /// Архив есть только у УПД, УПДи, УКД и УКДи.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>XML документа строкой и байтами как есть, подпись как есть.</returns>
        EdoSignedDocumentInfo GetIncomingSignedDocumentInfo(TrueApiEdoProviderOption settings, string documentId)
            => GetIncomingSignedDocumentInfoAsync(settings, documentId).GetAwaiter().GetResult();

        /// <summary>
        /// Получение файла входящего документа с его открепленной подписью.
        /// </summary>
        /// <remarks>
        /// Скачивает ZIP-архив документа с подписями и квитанциями. Из архива берётся файл, по которому назван архив, а без имени архива - информация продавца УПД / УПДи.
        /// Архив есть только у УПД, УПДи, УКД и УКДи.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>XML документа строкой и байтами как есть, подпись как есть.</returns>
        Task<EdoSignedDocumentInfo> GetIncomingSignedDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentId);

        /// <summary>
        /// Получение файла исходящего документа с его открепленной подписью.
        /// </summary>
        /// <remarks>
        /// Скачивает ZIP-архив документа с подписями и квитанциями. Из архива берётся файл, по которому назван архив, а без имени архива - информация продавца УПД / УПДи.
        /// Архив есть только у УПД, УПДи, УКД и УКДи.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>XML документа строкой и байтами как есть, подпись как есть.</returns>
        EdoSignedDocumentInfo GetOutgoingSignedDocumentInfo(TrueApiEdoProviderOption settings, string documentId)
            => GetOutgoingSignedDocumentInfoAsync(settings, documentId).GetAwaiter().GetResult();

        /// <summary>
        /// Получение файла исходящего документа с его открепленной подписью.
        /// </summary>
        /// <remarks>
        /// Скачивает ZIP-архив документа с подписями и квитанциями. Из архива берётся файл, по которому назван архив, а без имени архива - информация продавца УПД / УПДи.
        /// Архив есть только у УПД, УПДи, УКД и УКДи.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>XML документа строкой и байтами как есть, подпись как есть.</returns>
        Task<EdoSignedDocumentInfo> GetOutgoingSignedDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentId);

        /// <summary>
        /// Получение сведений из входящего файла информации продавца УПД / УПДи.
        /// </summary>
        /// <remarks>
        /// Скачивает файл с подписью, как <see cref="GetIncomingSignedDocumentInfoAsync"/>, и разбирает XML формата 970.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>Сведения из файла продавца и его подпись.</returns>
        EdoSellerDocumentInfo GetIncomingSellerDocumentInfo(TrueApiEdoProviderOption settings, string documentId)
            => GetIncomingSellerDocumentInfoAsync(settings, documentId).GetAwaiter().GetResult();

        /// <summary>
        /// Получение сведений из входящего файла информации продавца УПД / УПДи.
        /// </summary>
        /// <remarks>
        /// Скачивает файл с подписью, как <see cref="GetIncomingSignedDocumentInfoAsync"/>, и разбирает XML формата 970.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>Сведения из файла продавца и его подпись.</returns>
        Task<EdoSellerDocumentInfo> GetIncomingSellerDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentId);

        /// <summary>
        /// Получение сведений из исходящего файла информации продавца УПД / УПДи.
        /// </summary>
        /// <remarks>
        /// Скачивает файл с подписью, как <see cref="GetOutgoingSignedDocumentInfoAsync"/>, и разбирает XML формата 970.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>Сведения из файла продавца и его подпись.</returns>
        EdoSellerDocumentInfo GetOutgoingSellerDocumentInfo(TrueApiEdoProviderOption settings, string documentId)
            => GetOutgoingSellerDocumentInfoAsync(settings, documentId).GetAwaiter().GetResult();

        /// <summary>
        /// Получение сведений из исходящего файла информации продавца УПД / УПДи.
        /// </summary>
        /// <remarks>
        /// Скачивает файл с подписью, как <see cref="GetOutgoingSignedDocumentInfoAsync"/>, и разбирает XML формата 970.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор документа в системе ЭДО Оператора.</param>
        /// <returns>Сведения из файла продавца и его подпись.</returns>
        Task<EdoSellerDocumentInfo> GetOutgoingSellerDocumentInfoAsync(TrueApiEdoProviderOption settings, string documentId);

        /// <summary>
        /// Подписание черновика исходящего документа: УПД, УПДи, УКД или УКДи. После подписания документ уходит получателю.
        /// </summary>
        /// <remarks>
        /// Скачивает файл черновика, как <see cref="GetOutgoingDocumentAsync"/>, подписывает его байты как есть открепленной подписью
        /// сертификатом <see cref="TrueApiEdoProviderOption.CertificateThumbprint"/> и отправляет подпись.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор черновика в системе ЭДО Оператора, полученный при его загрузке.</param>
        void SignOutgoingDocument(TrueApiEdoProviderOption settings, string documentId)
            => SignOutgoingDocumentAsync(settings, documentId).GetAwaiter().GetResult();

        /// <summary>
        /// Подписание черновика исходящего документа: УПД, УПДи, УКД или УКДи. После подписания документ уходит получателю.
        /// </summary>
        /// <remarks>
        /// Скачивает файл черновика, как <see cref="GetOutgoingDocumentAsync"/>, подписывает его байты как есть открепленной подписью
        /// сертификатом <see cref="TrueApiEdoProviderOption.CertificateThumbprint"/> и отправляет подпись.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="documentId">Идентификатор черновика в системе ЭДО Оператора, полученный при его загрузке.</param>
        Task SignOutgoingDocumentAsync(TrueApiEdoProviderOption settings, string documentId);

        /// <summary>
        /// Отправка информации продавца УПД / УПДи формата 970: формирование XML, подпись и загрузка в «ЭДО Лайт».
        /// </summary>
        /// <remarks>
        /// Если задан <see cref="EdoSellerUniversalTransferDocument.RevisionNumber"/>, документ загружается как УПДи.
        /// Черновик не подписывается и остаётся у отправителя, его потом отправляет <see cref="SignOutgoingDocumentAsync"/>.
        /// Иначе файл подписывается сертификатом <see cref="TrueApiEdoProviderOption.CertificateThumbprint"/> и сразу уходит получателю.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="document">Информация продавца.</param>
        /// <param name="isDraft">Загрузить без подписи как черновик, не отправляя получателю.</param>
        /// <returns>Идентификатор документа в «ЭДО Лайт» и отправленный файл.</returns>
        /// <exception cref="EdoPostDocumentException">Ошибка подписи или загрузки сформированного файла: XML - в исключении.</exception>
        EdoPostDocumentResult PostUniversalTransferDocument(TrueApiEdoProviderOption settings, EdoSellerUniversalTransferDocument document, bool isDraft = false)
            => PostUniversalTransferDocumentAsync(settings, document, isDraft).GetAwaiter().GetResult();

        /// <summary>
        /// Отправка информации продавца УПД / УПДи формата 970: формирование XML, подпись и загрузка в «ЭДО Лайт».
        /// </summary>
        /// <remarks>
        /// Если задан <see cref="EdoSellerUniversalTransferDocument.RevisionNumber"/>, документ загружается как УПДи.
        /// Черновик не подписывается и остаётся у отправителя, его потом отправляет <see cref="SignOutgoingDocumentAsync"/>.
        /// Иначе файл подписывается сертификатом <see cref="TrueApiEdoProviderOption.CertificateThumbprint"/> и сразу уходит получателю.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="document">Информация продавца.</param>
        /// <param name="isDraft">Загрузить без подписи как черновик, не отправляя получателю.</param>
        /// <returns>Идентификатор документа в «ЭДО Лайт» и отправленный файл.</returns>
        /// <exception cref="EdoPostDocumentException">Ошибка подписи или загрузки сформированного файла: XML - в исключении.</exception>
        Task<EdoPostDocumentResult> PostUniversalTransferDocumentAsync(TrueApiEdoProviderOption settings, EdoSellerUniversalTransferDocument document, bool isDraft = false);

        /// <summary>
        /// Отправка информации покупателя (титула покупателя) к входящему УПД / УПДи формата 970: формирование XML, подпись и загрузка в «ЭДО Лайт».
        /// </summary>
        /// <remarks>
        /// Скачивает файл продавца <see cref="EdoBuyerUniversalTransferDocument.DocumentId"/>, как <see cref="GetIncomingSellerDocumentInfoAsync"/>:
        /// из него берутся реквизиты документа, подпись продавца, отправитель и получатель, а по наличию исправления выбирается метод для УПД или УПДи.
        /// Файл подписывается сертификатом <see cref="TrueApiEdoProviderOption.CertificateThumbprint"/> и сразу уходит продавцу.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="document">Информация покупателя.</param>
        /// <returns>Идентификатор, который вернул «ЭДО Лайт», и отправленный файл.</returns>
        /// <exception cref="EdoPostDocumentException">Ошибка подписи или загрузки сформированного файла: XML - в исключении.</exception>
        EdoPostDocumentResult ReceiptUniversalTransferDocument(TrueApiEdoProviderOption settings, EdoBuyerUniversalTransferDocument document)
            => ReceiptUniversalTransferDocumentAsync(settings, document).GetAwaiter().GetResult();

        /// <summary>
        /// Отправка информации покупателя (титула покупателя) к входящему УПД / УПДи формата 970: формирование XML, подпись и загрузка в «ЭДО Лайт».
        /// </summary>
        /// <remarks>
        /// Скачивает файл продавца <see cref="EdoBuyerUniversalTransferDocument.DocumentId"/>, как <see cref="GetIncomingSellerDocumentInfoAsync"/>:
        /// из него берутся реквизиты документа, подпись продавца, отправитель и получатель, а по наличию исправления выбирается метод для УПД или УПДи.
        /// Файл подписывается сертификатом <see cref="TrueApiEdoProviderOption.CertificateThumbprint"/> и сразу уходит продавцу.
        /// </remarks>
        /// <param name="settings">Настройки провайдера.</param>
        /// <param name="document">Информация покупателя.</param>
        /// <returns>Идентификатор, который вернул «ЭДО Лайт», и отправленный файл.</returns>
        /// <exception cref="EdoPostDocumentException">Ошибка подписи или загрузки сформированного файла: XML - в исключении.</exception>
        Task<EdoPostDocumentResult> ReceiptUniversalTransferDocumentAsync(TrueApiEdoProviderOption settings, EdoBuyerUniversalTransferDocument document);
    }
}
