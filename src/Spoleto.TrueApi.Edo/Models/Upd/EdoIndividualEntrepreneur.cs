namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения об индивидуальном предпринимателе (СвИП).
    /// </summary>
    public record EdoIndividualEntrepreneur : EdoPerson
    {
        /// <summary>
        /// ИНН (ИННФЛ).
        /// </summary>
        public required string Inn { get; set; }

        /// <summary>
        /// Реквизиты свидетельства о государственной регистрации ИП (СвГосРегИП).
        /// </summary>
        public string? IndividualEntrepreneurRegistrationCertificate { get; set; }

        /// <summary>
        /// ОГРНИП (ОГРНИП).
        /// </summary>
        public string? Ogrnip { get; set; }

        /// <summary>
        /// Дата присвоения ОГРНИП (ДатаОГРНИП).
        /// </summary>
        public DateTime? OgrnipDate { get; set; }

        /// <summary>
        /// Иные сведения, идентифицирующие физическое лицо (ИныеСвед).
        /// </summary>
        public string? OtherInfo { get; set; }

        public override string ToString() => $"ИП {base.ToString()}";
    }
}
