namespace Spoleto.TrueApi.Edo.Enums
{
    /// <summary>
    /// Справочник «Статусная модель» документов «ЭДО Лайт».
    /// </summary>
    public enum EdoDocumentStatus
    {
        /// <summary>
        /// Черновик.
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Отправлен.
        /// </summary>
        Sent = 1,

        /// <summary>
        /// Доставлен (подпись не требуется) / Подпись не требуется.
        /// </summary>
        DeliveredNoSignatureRequired = 2,

        /// <summary>
        /// Доставлен (ожидается подпись) / Требуется подпись.
        /// </summary>
        DeliveredSignatureRequired = 3,

        /// <summary>
        /// Подписан.
        /// </summary>
        Signed = 4,

        /// <summary>
        /// Подписан с расхождениями / Отклонён.
        /// </summary>
        Declined = 5,

        /// <summary>
        /// Уточнён: на документ сформирован исправленный или корректировочный документ.
        /// </summary>
        Clarified = 7,

        /// <summary>
        /// Требуется уточнение / Ожидается уточнение.
        /// </summary>
        ClarificationRequired = 8,

        /// <summary>
        /// Ожидается отправка: документ отправляется в роуминг.
        /// </summary>
        AwaitingSending = 11,

        /// <summary>
        /// Просмотрен (подпись не требуется) / Подпись не требуется (документ просмотрен).
        /// </summary>
        ReviewedNoSignatureRequired = 12,

        /// <summary>
        /// Просмотрен (ожидается подпись) / Требуется подпись (документ просмотрен).
        /// </summary>
        ReviewedSignatureRequired = 13,

        /// <summary>
        /// Требуется уточнение (запрос на уточнение просмотрен) / Запрос на уточнение просмотрен (ожидается уточнение).
        /// </summary>
        ClarificationRequiredClarificationRequestReviewed = 14,

        /// <summary>
        /// Подписан с расхождениями (запрос просмотрен) / Отклонён (запрос просмотрен).
        /// </summary>
        DeclinedRequestReviewed = 15,

        /// <summary>
        /// Ожидается аннулирование.
        /// </summary>
        AnnulmentExpected = 16,

        /// <summary>
        /// Требуется аннулирование.
        /// </summary>
        AnnulmentRequired = 17,

        /// <summary>
        /// Аннулирован.
        /// </summary>
        Annulled = 18,

        /// <summary>
        /// Отказано в аннулировании.
        /// </summary>
        AnnulmentDeclined = 19,

        /// <summary>
        /// Ошибка отправки: код ответа в диапазоне 400-499.
        /// </summary>
        SendingClientError = 41,

        /// <summary>
        /// Ошибка отправки: код ответа в диапазоне 500-599.
        /// </summary>
        SendingServerError = 42,

        /// <summary>
        /// Ошибка обработки: оператор ЭДО получателя не нашёл документ, к которому отправлен текущий.
        /// </summary>
        ProcessingErrorParentNotFound = 43,

        /// <summary>
        /// Ошибка обработки: неизвестная ошибка у оператора ЭДО получателя.
        /// </summary>
        ProcessingError = 44,

        /// <summary>
        /// Подписан: Отправлен в ГИС МТ.
        /// </summary>
        SignedSentToGisMT = 61,

        /// <summary>
        /// Подписан: Не принят в ГИС МТ.
        /// </summary>
        SignedRejectedByGisMT = 62,

        /// <summary>
        /// Подписан: Отправка в ГИС МТ.
        /// </summary>
        SignedSendingToGisMT = 63,

        /// <summary>
        /// Аннулирован: Отправлен в ГИС МТ.
        /// </summary>
        AnnulledSentToGisMT = 64,

        /// <summary>
        /// Аннулирован: Не принят в ГИС МТ.
        /// </summary>
        AnnulledRejectedByGisMT = 65,

        /// <summary>
        /// Аннулирован: Отправка в ГИС МТ.
        /// </summary>
        AnnulledSendingToGisMT = 66
    }
}
