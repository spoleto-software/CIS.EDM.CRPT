namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Информация продавца об обстоятельствах закупок для государственных и муниципальных нужд (ИнфПродЗаГосКазн).
    /// </summary>
    public record EdoSellerInfoCircumPublicProc
    {
        /// <summary>
        /// Дата государственного контракта (ДатаГосКонт).
        /// </summary>
        public required DateTime DateStateContract { get; set; }

        /// <summary>
        /// Номер государственного контракта (НомерГосКонт).
        /// </summary>
        public required string NumberStateContract { get; set; }

        /// <summary>
        /// Номер лицевого счета продавца (ЛицСчетПрод).
        /// </summary>
        public string? PersonalAccountSeller { get; set; }

        /// <summary>
        /// Код по бюджетной классификации продавца (КодПродБюджКласс).
        /// </summary>
        public string? SellerBudgetClassCode { get; set; }

        /// <summary>
        /// Код цели продавца (КодЦелиПрод).
        /// </summary>
        public string? SellerTargetCode { get; set; }

        /// <summary>
        /// Код территориального органа Федерального казначейства продавца (КодКазначПрод).
        /// </summary>
        public string? SellerTreasuryCode { get; set; }

        /// <summary>
        /// Наименование территориального органа Федерального казначейства продавца (НаимКазначПрод).
        /// </summary>
        public string? SellerTreasuryName { get; set; }
    }
}
