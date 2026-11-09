namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о физическом лице (СвФЛУч).
    /// </summary>
    public record EdoPhysicalPerson : EdoPerson
    {
        /// <summary>
        /// ИНН (ИННФЛ).
        /// </summary>
        public string? Inn { get; set; }

        /// <summary>
        /// Идентификация статуса лица (ИдСтатЛ): «0» - покупатель при розничной продаже, «1» - иное.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Иные сведения, идентифицирующие физическое лицо (ИныеСвед).
        /// </summary>
        public string? OtherInfo { get; set; }
    }
}
