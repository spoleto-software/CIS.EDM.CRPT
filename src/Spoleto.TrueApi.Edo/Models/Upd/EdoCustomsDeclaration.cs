namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о декларации на товары (СвДТ).
    /// </summary>
    public record EdoCustomsDeclaration
    {
        /// <summary>
        /// Цифровой код страны происхождения по ОКСМ, 980 - Евросоюз, 981 - ЕАЭС; графа 10 счета-фактуры (КодПроисх).
        /// </summary>
        public string? CountryCode { get; set; }

        /// <summary>
        /// Регистрационный номер декларации на товары, графа 11 счета-фактуры (НомерДТ).
        /// </summary>
        public string? DeclarationNumber { get; set; }

        public override string ToString() => $"{DeclarationNumber} ({CountryCode})";
    }
}
