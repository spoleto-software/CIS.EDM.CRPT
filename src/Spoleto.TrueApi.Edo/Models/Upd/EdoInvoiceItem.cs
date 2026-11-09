using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Строка таблицы счета-фактуры: отгруженный товар, работа, услуга или имущественное право (СведТов). Номер строки проставляется по порядку.
    /// </summary>
    public record EdoInvoiceItem
    {
        /// <summary>
        /// Наименование товара (НаимТов).
        /// </summary>
        public required string ProductName { get; set; }

        /// <summary>
        /// Код единицы измерения по ОКЕИ или «0000», графа 2 счета-фактуры (ОКЕИ_Тов).
        /// </summary>
        public string? UnitCode { get; set; }

        /// <summary>
        /// Наименование единицы измерения, графа 2а счета-фактуры (НаимЕдИзм).
        /// </summary>
        public string? UnitName { get; set; }

        /// <summary>
        /// Количество, графа 3 счета-фактуры (КолТов). Ноль не формируется.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Цена за единицу измерения, графа 4 счета-фактуры (ЦенаТов). Ноль не формируется.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Стоимость без налога, графа 5 счета-фактуры (СтТовБезНДС). Ноль не формируется.
        /// </summary>
        public decimal SumWithoutVat { get; set; }

        /// <summary>
        /// Налоговая ставка, графа 7 счета-фактуры (НалСт).
        /// </summary>
        public required EdoTaxRate TaxRate { get; set; }

        /// <summary>
        /// Стоимость с налогом, графа 9 счета-фактуры (СтТовУчНал).
        /// </summary>
        public required decimal Sum { get; set; }

        /// <summary>
        /// Сумма акциза, графа 6 счета-фактуры (Акциз/СумАкциз). Если не задана - «без акциза».
        /// </summary>
        public decimal? Excise { get; set; }

        /// <summary>
        /// Сумма налога, графа 8 счета-фактуры (СумНал). Обязательна, кроме ставки «без НДС».
        /// </summary>
        public decimal? Vat { get; set; }

        /// <summary>
        /// Сведения о декларациях на товары, графы 10-11 счета-фактуры (СвДТ).
        /// </summary>
        public List<EdoCustomsDeclaration>? CustomsDeclarationList { get; set; }

        /// <summary>
        /// Дополнительные сведения о товаре: признак, артикул, ГТИН, коды маркировки и т. п. (ДопСведТов).
        /// </summary>
        public EdoInvoiceItemAdditionalInfo? AdditionalInfo { get; set; }

        /// <summary>
        /// Информационное поле факта хозяйственной жизни 2 (ИнфПолФХЖ2).
        /// </summary>
        public List<EdoOtherEconomicInfoItem>? OtherEconomicInfoItemList { get; set; }

        public override string ToString() => $"{ProductName}: {Sum}";
    }
}
