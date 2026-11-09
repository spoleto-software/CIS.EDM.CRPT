namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Дополнительные сведения о товаре, подлежащем идентификации и учету в государственной информационной системе (СвГосСист).
    /// </summary>
    public record EdoGovernmentSystemAdditionalInfo
    {
        /// <summary>
        /// Наименование государственной информационной системы (НаимГосСист).
        /// </summary>
        public required string InformationSystemName { get; set; }

        /// <summary>
        /// Учетная единица в государственной информационной системе (УчетЕд).
        /// </summary>
        public string? AccountingUnit { get; set; }

        /// <summary>
        /// Иная информация о товаре (ИнаяИнф).
        /// </summary>
        public string? OtherInfo { get; set; }

        /// <summary>
        /// Идентификационные номера учетных единиц (ИдНомУчетЕд).
        /// </summary>
        public required List<string> AccountingUnitIds { get; set; }
    }
}
