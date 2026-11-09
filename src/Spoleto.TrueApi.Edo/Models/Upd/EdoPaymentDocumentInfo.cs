namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о платежно-расчетном документе, строка 5 счета-фактуры (СвПРД).
    /// </summary>
    public record EdoPaymentDocumentInfo
    {
        /// <summary>
        /// Номер платежно-расчетного документа (НомерПРД).
        /// </summary>
        public string? Number { get; set; }

        /// <summary>
        /// Дата составления платежно-расчетного документа (ДатаПРД).
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// Сумма (СуммаПРД).
        /// </summary>
        public decimal? Total { get; set; }
    }
}
