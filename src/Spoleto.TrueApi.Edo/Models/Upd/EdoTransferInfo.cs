namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о факте отгрузки товаров, выполнения работ, передачи имущественных прав (СвПродПер).
    /// </summary>
    public record EdoTransferInfo
    {
        /// <summary>
        /// Сведения о передаче (СвПер).
        /// </summary>
        public required EdoTransferDetails TransferDetails { get; set; }

        /// <summary>
        /// Информационное поле факта хозяйственной жизни 3 (ИнфПолФХЖ3).
        /// </summary>
        public EdoOtherEconomicInfo? OtherEconomicInfo { get; set; }
    }
}
