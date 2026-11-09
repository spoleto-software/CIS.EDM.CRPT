namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о лице, передавшем товар (СвЛицПер). Заполняется одно свойство.
    /// </summary>
    public record EdoSenderPerson
    {
        /// <summary>
        /// Работник организации продавца (РабОргПрод).
        /// </summary>
        public EdoEmployee? Employee { get; set; }

        /// <summary>
        /// Иное лицо (ИнЛицо).
        /// </summary>
        public EdoOtherIssuer? OtherIssuer { get; set; }
    }
}
