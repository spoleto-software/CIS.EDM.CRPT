namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения об участнике факта хозяйственной жизни (УчастникТип): продавец, покупатель, грузоотправитель, грузополучатель, фактор.
    /// </summary>
    public record EdoOrganization
    {
        /// <summary>
        /// Код по ОКПО (ОКПО).
        /// </summary>
        public string? Okpo { get; set; }

        /// <summary>
        /// Код по ОКОПФ (КодОПФ).
        /// </summary>
        public string? OpfCode { get; set; }

        /// <summary>
        /// Полное наименование организационно-правовой формы (ПолнНаимОПФ).
        /// </summary>
        public string? OpfName { get; set; }

        /// <summary>
        /// Структурное подразделение (СтруктПодр).
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Информация для участника документооборота (ИнфДляУчаст).
        /// </summary>
        public string? OrganizationAdditionalInfo { get; set; }

        /// <summary>
        /// Краткое наименование (СокрНаим).
        /// </summary>
        public string? ShortName { get; set; }

        /// <summary>
        /// Идентификационные сведения (ИдСв): заполняется один из видов.
        /// </summary>
        public required EdoOrganizationIdentificationInfo OrganizationIdentificationInfo { get; set; }

        /// <summary>
        /// Адрес (Адрес).
        /// </summary>
        public EdoAddress? Address { get; set; }

        /// <summary>
        /// Банковские реквизиты (БанкРекв).
        /// </summary>
        public EdoBankAccountDetails? BankAccountDetails { get; set; }

        /// <summary>
        /// Контактные данные (Контакт).
        /// </summary>
        public EdoContact? Contact { get; set; }

        public override string ToString() => ShortName ?? OrganizationIdentificationInfo.ToString();
    }
}
