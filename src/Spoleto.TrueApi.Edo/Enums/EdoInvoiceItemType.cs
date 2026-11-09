namespace Spoleto.TrueApi.Edo.Enums
{
    /// <summary>
    /// Признак Товар/Работа/Услуга/Право/Иное (ПрТовРаб).
    /// </summary>
    public enum EdoInvoiceItemType
    {
        /// <summary>
        /// Не указан: атрибут не формируется.
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Имущество.
        /// </summary>
        Property = 1,

        /// <summary>
        /// Работа.
        /// </summary>
        Job = 2,

        /// <summary>
        /// Услуга.
        /// </summary>
        Service = 3,

        /// <summary>
        /// Имущественные права.
        /// </summary>
        PropertyRights = 4,

        /// <summary>
        /// Иное.
        /// </summary>
        Other = 5
    }
}
