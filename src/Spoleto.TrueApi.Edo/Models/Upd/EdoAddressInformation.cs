namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Адрес за пределами Российской Федерации (АдрИнф).
    /// </summary>
    public record EdoAddressInformation
    {
        /// <summary>
        /// Код страны по ОКСМ (КодСтр).
        /// </summary>
        public required string CountryCode { get; set; }

        /// <summary>
        /// Наименование страны (НаимСтран).
        /// </summary>
        public required string CountryName { get; set; }

        /// <summary>
        /// Адрес текстом (АдрТекст).
        /// </summary>
        public required string Address { get; set; }

        public override string ToString() => $"{CountryName}, {Address}";
    }
}
