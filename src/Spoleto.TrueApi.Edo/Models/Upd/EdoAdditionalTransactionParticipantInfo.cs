using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Дополнительные сведения об участниках факта хозяйственной жизни, основаниях и обстоятельствах его проведения (ДопСвФХЖ1).
    /// </summary>
    public record EdoAdditionalTransactionParticipantInfo
    {
        /// <summary>
        /// Идентификатор государственного контракта, договора (соглашения), строка 8 счета-фактуры (ИдГосКон).
        /// </summary>
        public string? GovernmentContractInfo { get; set; }

        /// <summary>
        /// Специальные обстоятельства формирования счета-фактуры (СпОбстФСЧФ). Формируется только при функции СЧФ.
        /// </summary>
        public EdoInvoiceFormationType InvoiceFormationType { get; set; }

        /// <summary>
        /// Специальные обстоятельства формирования УПД, включающего счет-фактуру (СпОбстФСЧФДОП). Формируется только при функции СЧФДОП.
        /// </summary>
        public string? UPDInvoiceFormationType { get; set; }

        /// <summary>
        /// Специальные обстоятельства формирования документа (СпОбстФДОП). Формируется только при функции ДОП.
        /// </summary>
        public string? UPDFormationType { get; set; }

        /// <summary>
        /// Виды обязательств (ВидОбяз).
        /// </summary>
        public List<EdoObligationInfo>? ObligationInfoList { get; set; }

        /// <summary>
        /// Информация продавца об обстоятельствах закупок для государственных и муниципальных нужд (ИнфПродЗаГосКазн).
        /// </summary>
        public EdoSellerInfoCircumPublicProc? SellerInfoCircumPublicProc { get; set; }

        /// <summary>
        /// Сведения о факторе (СвФактор).
        /// </summary>
        public EdoOrganization? FactorInfo { get; set; }

        /// <summary>
        /// Основание уступки денежного требования (ОснУстДенТреб).
        /// </summary>
        public EdoDocumentDetails? MainAssignMonetaryClaim { get; set; }

        /// <summary>
        /// Сопроводительные документы, уточняющие обстоятельства факта хозяйственной жизни (СопрДокФХЖ).
        /// </summary>
        public List<EdoDocumentDetails>? SupportingDocumentList { get; set; }
    }
}
