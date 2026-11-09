namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Работник организации: продавца, передавший товар (РабОргПрод), или покупателя, принявший товар (РабОргПок).
    /// </summary>
    public record EdoEmployee : EdoPerson
    {
        /// <summary>
        /// Должность (Должность).
        /// </summary>
        public required string JobTitle { get; set; }

        /// <summary>
        /// Иные сведения, идентифицирующие физическое лицо (ИныеСвед).
        /// </summary>
        public string? OtherInfo { get; set; }
    }
}
