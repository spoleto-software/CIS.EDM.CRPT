using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Информация покупателя об обстоятельствах закупок для государственных и муниципальных нужд
    /// (для учета Федеральным казначейством денежных обязательств) (ИнфПокЗаГоскКазн).
    /// </summary>
    public record EdoBuyerInfoCircumPublicProc
    {
        /// <summary>
        /// Идентификационный код закупки (ИдКодЗак).
        /// </summary>
        public string? PurchasingIdentificationCode { get; set; }

        /// <summary>
        /// Номер лицевого счета покупателя (ЛицСчетПок).
        /// </summary>
        public required string PersonalAccountBuyer { get; set; }

        /// <summary>
        /// Наименование финансового органа покупателя (НаимФинОргПок).
        /// </summary>
        public required string BuyerFinancialAuthorityName { get; set; }

        /// <summary>
        /// Номер реестровой записи покупателя по Реестру участников бюджетного процесса,
        /// а также юридических лиц, не являющихся участниками бюджетного процесса (НомРеестрЗапПок).
        /// </summary>
        public required string BuyerBudgetRegisterNumber { get; set; }

        /// <summary>
        /// Учетный номер бюджетного обязательства покупателя (УчНомБюдОбязПок).
        /// </summary>
        public string? BuyerBudgetObligationAccountNumber { get; set; }

        /// <summary>
        /// Код территориального органа Федерального казначейства покупателя (КодКазначПок).
        /// </summary>
        public string? BuyerTreasuryCode { get; set; }

        /// <summary>
        /// Наименование территориального органа Федерального казначейства покупателя (НаимКазначПок).
        /// </summary>
        public string? BuyerTreasuryName { get; set; }

        /// <summary>
        /// Код покупателя по ОКТМО (ОКТМОПок).
        /// </summary>
        public required string BuyerMunicipalCode { get; set; }

        /// <summary>
        /// Код места поставки по ОКТМО (ОКТМОМесПост).
        /// </summary>
        public string? DeliveryMunicipalCode { get; set; }

        /// <summary>
        /// Предельная дата оплаты (ДатаОплПред).
        /// </summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// Учетный номер денежного обязательства (УчНомДенОбяз).
        /// </summary>
        /// <remarks>
        /// Обязателен при изменении ранее направленного в Федеральное казначейство документа, по которому поставлено на учет денежное обязательство.
        /// </remarks>
        public string? FinancialObligationAccountNumber { get; set; }

        /// <summary>
        /// Очередность платежа (ОчерПлат). Обязательна при <see cref="PaymentDate"/>.
        /// </summary>
        public string? PaymentOrder { get; set; }

        /// <summary>
        /// Вид платежа (ВидПлат). Обязателен при <see cref="PaymentDate"/>.
        /// </summary>
        public EdoPaymentType? PaymentType { get; set; }

        /// <summary>
        /// Информация для сведений о денежном обязательстве (ИнфСведДенОбяз).
        /// </summary>
        public required List<EdoFinancialObligationInfo> FinancialObligationInfoList { get; set; }
    }
}
