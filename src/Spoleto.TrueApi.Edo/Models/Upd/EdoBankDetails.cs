namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о банке (СвБанк).
    /// </summary>
    public record EdoBankDetails
    {
        /// <summary>
        /// Наименование банка (НаимБанк).
        /// </summary>
        public string? BankName { get; set; }

        /// <summary>
        /// БИК (БИК).
        /// </summary>
        public string? BankId { get; set; }

        /// <summary>
        /// Корреспондентский счет банка (КорСчет).
        /// </summary>
        public string? CorrespondentAccount { get; set; }

        public override string ToString() => BankName ?? BankId ?? string.Empty;
    }
}
