namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Транспортировка (Тран).
    /// </summary>
    public record EdoTransportation
    {
        /// <summary>
        /// Сведения о транспортировке и грузе (СвТран).
        /// </summary>
        public string? TransferTextInfo { get; set; }

        /// <summary>
        /// Код условий поставки по Инкотермс (Инкотермс).
        /// </summary>
        public string? Incoterms { get; set; }

        /// <summary>
        /// Версия Инкотермс (ВерИнкотермс).
        /// </summary>
        public string? IncotermsVersion { get; set; }
    }
}
