namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Физическое лицо, которому доверена отгрузка товаров (ФЛПер) или их принятие (ФЛПрин).
    /// </summary>
    public record EdoTransferPhysicalPerson : EdoPerson
    {
        /// <summary>
        /// ИНН физического лица (ИННФЛПер, ИННФЛПрин).
        /// </summary>
        public string? PersonInn { get; set; }

        /// <summary>
        /// Иные сведения, идентифицирующие физическое лицо (ИныеСвед).
        /// </summary>
        public string? OtherInfo { get; set; }

        /// <summary>
        /// Основание, по которому физическому лицу доверена отгрузка или принятие товаров (ОснДоверФЛ).
        /// </summary>
        public EdoDocumentDetails? PersonBase { get; set; }
    }
}
