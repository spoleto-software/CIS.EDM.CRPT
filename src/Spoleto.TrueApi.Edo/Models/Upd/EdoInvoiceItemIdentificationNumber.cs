namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Номера средств идентификации товаров (НомСредИдентТов).
    /// </summary>
    /// <remarks>
    /// Заполняется хотя бы одно из: <see cref="PackageId"/>, <see cref="MarkedProductQuantity"/>, <see cref="MarkItems"/>, <see cref="SecondaryPackageItems"/>.
    /// Если заданы <see cref="MarkItems"/>, <see cref="SecondaryPackageItems"/> не формируются.
    /// </remarks>
    public record EdoInvoiceItemIdentificationNumber
    {
        /// <summary>
        /// Уникальный идентификатор транспортной упаковки, например, SSCC (ИдентТрансУпак).
        /// </summary>
        public string? PackageId { get; set; }

        /// <summary>
        /// Количество товара в единицах измерения маркированного товара (КолВедМарк).
        /// </summary>
        public string? MarkedProductQuantity { get; set; }

        /// <summary>
        /// Производственная партия (ПрПартМарк).
        /// </summary>
        public string? ProductionBatchCode { get; set; }

        /// <summary>
        /// Коды маркировки, контрольные идентификационные знаки (КИЗ).
        /// </summary>
        public List<string>? MarkItems { get; set; }

        /// <summary>
        /// Идентификаторы вторичной (потребительской) или третичной (заводской, транспортной) упаковки (НомУпак).
        /// </summary>
        public List<string>? SecondaryPackageItems { get; set; }
    }
}
