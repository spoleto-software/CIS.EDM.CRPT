namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о лице, принявшем товары (груз) (СвЛицПрин). Заполняется одно свойство.
    /// </summary>
    public record EdoReceiverPerson
    {
        /// <summary>
        /// Работник организации покупателя (РабОргПок).
        /// </summary>
        public EdoEmployee? Employee { get; set; }

        /// <summary>
        /// Иное лицо (ИнЛицо): представитель организации, которой доверено принятие товаров (ПредОргПрин),
        /// или физическое лицо, которому доверено принятие товаров (ФЛПрин).
        /// </summary>
        public EdoOtherIssuer? OtherIssuer { get; set; }
    }
}
