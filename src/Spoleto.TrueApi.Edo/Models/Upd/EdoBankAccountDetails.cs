namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Банковские реквизиты (БанкРекв).
    /// </summary>
    public record EdoBankAccountDetails
    {
        /// <summary>
        /// Номер банковского счета (НомерСчета).
        /// </summary>
        public string? BankAccountNumber { get; set; }

        /// <summary>
        /// Сведения о банке (СвБанк).
        /// </summary>
        public EdoBankDetails? BankDetails { get; set; }

        public override string ToString() => $"{BankAccountNumber}, {BankDetails}";
    }
}
