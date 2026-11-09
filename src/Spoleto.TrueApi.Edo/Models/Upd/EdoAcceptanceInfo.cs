namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о принятии товаров (результатов выполненных работ), имущественных прав (о подтверждении факта оказания услуг) (СвПрин).
    /// </summary>
    public record EdoAcceptanceInfo
    {
        /// <summary>
        /// Содержание операции (СодОпер), например, «Товары принял без претензий».
        /// </summary>
        /// <remarks>
        /// При итоге приемки <see cref="Enums.EdoOperationNameCode.NotAccepted"/> не указывается.
        /// </remarks>
        public string? OperationName { get; set; }

        /// <summary>
        /// Дата принятия товаров (результатов выполненных работ), имущественных прав (подтверждения факта оказания услуг) (ДатаПрин).
        /// </summary>
        public required DateTime Date { get; set; }

        /// <summary>
        /// Код содержания операции (КодСодОпер): итог приемки и документ о расхождениях.
        /// </summary>
        public EdoOperationNameInfo OperationNameInfo { get; set; } = new();

        /// <summary>
        /// Сведения о лице, принявшем товары (груз) (СвЛицПрин).
        /// </summary>
        public EdoReceiverPerson? ReceiverPerson { get; set; }
    }
}
