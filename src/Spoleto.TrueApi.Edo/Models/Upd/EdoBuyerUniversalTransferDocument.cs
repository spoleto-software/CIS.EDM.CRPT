namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Информация покупателя УПД / УПДи по формату приказа ФНС России от 19.12.2023 № ЕД-7-26/970@, версия формата 5.03 (Файл).
    /// </summary>
    /// <remarks>
    /// Код КНД (1115132) и версия формата (5.03) - константы файла и в модель не входят.
    /// Сведения о файле продавца (ИдИнфПрод, реквизиты документа в СодФХЖ4, УИД), отправитель и получатель
    /// берутся из входящего документа <see cref="DocumentId"/> при отправке.
    /// </remarks>
    public record EdoBuyerUniversalTransferDocument
    {
        /// <summary>
        /// Идентификатор входящего документа (информации продавца) в «ЭДО Лайт», к которому формируется информация покупателя.
        /// </summary>
        public required string DocumentId { get; set; }

        /// <summary>
        /// Идентификатор файла (ИдФайл), он же имя файла без расширения.
        /// </summary>
        /// <remarks>
        /// Если не задан, формируется при отправке: ON_NSCHFDOPPOK_{получатель}_{отправитель}_{ГГГГММДД}_{GUID}_N2_N3_N4_N5_N6_00,
        /// где получатель и отправитель - отправитель и получатель файла продавца, N2-N6 - как в имени файла продавца.
        /// </remarks>
        public string? FileId { get; set; }

        /// <summary>
        /// Версия программы, с помощью которой сформирован файл (ВерсПрог).
        /// </summary>
        public string ApplicationCreator { get; set; } = "Spoleto.TrueApi.Edo";

        /// <summary>
        /// Дата и время формирования файла (ДатаИнфПок, ВремИнфПок), UTC.
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Экономический субъект - составитель файла (НаимЭконСубСост): в файл попадает его наименование с ИНН и КПП.
        /// </summary>
        public required EdoOrganization DocumentCreator { get; set; }

        /// <summary>
        /// Основание, по которому экономический субъект является составителем файла (ОснДоверОргСост).
        /// </summary>
        /// <remarks>
        /// Обязательно, если составитель не покупатель.
        /// </remarks>
        public EdoDocumentDetails? DocumentCreatorBase { get; set; }

        /// <summary>
        /// Вид операции (СодФХЖ4/ВидОпер): позволяет в автоматизированном режиме определить порядок использования информации документа у покупателя.
        /// </summary>
        public string? OperationType { get; set; }

        /// <summary>
        /// Сведения о принятии товаров (результатов выполненных работ), имущественных прав (о подтверждении факта оказания услуг) (СвПрин).
        /// </summary>
        public required EdoAcceptanceInfo AcceptanceInfo { get; set; }

        /// <summary>
        /// Информационное поле факта хозяйственной жизни 4 (ИнфПолФХЖ4).
        /// </summary>
        public EdoOtherEconomicInfo? OtherEconomicInfo { get; set; }

        /// <summary>
        /// Информация покупателя об обстоятельствах закупок для государственных и муниципальных нужд (ИнфПокЗаГоскКазн).
        /// </summary>
        /// <remarks>
        /// Обязательна при закупках для обеспечения государственных и муниципальных нужд
        /// и (или) для формирования сведений о денежном обязательстве Федеральным казначейством.
        /// </remarks>
        public EdoBuyerInfoCircumPublicProc? BuyerInfoCircumPublicProc { get; set; }

        /// <summary>
        /// Лица, подписывающие файл (Подписант).
        /// </summary>
        public required List<EdoSigner> Signers { get; set; }

        public override string ToString() => $"Информация покупателя к документу {DocumentId}";
    }
}
