namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Иное лицо, передавшее товар в информации продавца или принявшее товар в информации покупателя (ИнЛицо). Заполняется одно свойство.
    /// </summary>
    public record EdoOtherIssuer
    {
        /// <summary>
        /// Представитель организации, которой доверена отгрузка товаров (ПредОргПер) или их принятие (ПредОргПрин).
        /// </summary>
        public EdoTransferOrganizationPerson? OrganizationPerson { get; set; }

        /// <summary>
        /// Физическое лицо, которому доверена отгрузка товаров (ФЛПер) или их принятие (ФЛПрин).
        /// </summary>
        public EdoTransferPhysicalPerson? PhysicalPerson { get; set; }
    }
}
