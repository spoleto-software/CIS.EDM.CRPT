namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Текстовая информация информационного поля (ТекстИнф, ИнфПолФХЖ2).
    /// </summary>
    public record EdoOtherEconomicInfoItem
    {
        /// <summary>
        /// Идентификатор (Идентиф).
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Значение (Значен).
        /// </summary>
        public required string Value { get; set; }

        public override string ToString() => $"{Id}: {Value}";
    }
}
