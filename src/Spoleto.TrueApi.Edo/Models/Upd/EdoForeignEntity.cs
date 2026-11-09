namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения об иностранном лице, не состоящем на учете в налоговых органах (СвИнНеУч, ДаннИно).
    /// </summary>
    public record EdoForeignEntity
    {
        /// <summary>
        /// Идентификация статуса (ИдСтат): «ИО» - иностранная организация, «ИГ» - иностранный гражданин.
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Код страны по ОКСМ (КодСтр).
        /// </summary>
        public required string CountryCode { get; set; }

        /// <summary>
        /// Наименование страны по ОКСМ (НаимСтран).
        /// </summary>
        public required string CountryName { get; set; }

        /// <summary>
        /// Полное наименование (Наим).
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Идентификатор иностранной организации или гражданина (Идентиф). Обязателен при отсутствии <see cref="OtherInfo"/>.
        /// </summary>
        public string? Identifier { get; set; }

        /// <summary>
        /// Иные сведения для идентификации (ИныеСвед). Обязательны при отсутствии <see cref="Identifier"/>.
        /// </summary>
        public string? OtherInfo { get; set; }

        public override string ToString() => Name;
    }
}
