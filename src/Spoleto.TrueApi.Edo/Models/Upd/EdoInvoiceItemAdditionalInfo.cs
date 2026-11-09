using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Дополнительные сведения об отгруженном товаре, работе, услуге или имущественном праве (ДопСведТов).
    /// </summary>
    public record EdoInvoiceItemAdditionalInfo
    {
        /// <summary>
        /// Признак Товар/Работа/Услуга/Право/Иное (ПрТовРаб).
        /// </summary>
        public EdoInvoiceItemType Type { get; set; }

        /// <summary>
        /// Дополнительная информация о признаке (ДопПризн).
        /// </summary>
        public string? AdditionalTypeInfo { get; set; }

        /// <summary>
        /// Заказанное количество (НадлОтп).
        /// </summary>
        public decimal? ItemToRelease { get; set; }

        /// <summary>
        /// Характеристика или описание товара (ХарактерТов).
        /// </summary>
        public string? Characteristic { get; set; }

        /// <summary>
        /// Сорт товара (СортТов).
        /// </summary>
        public string? Kind { get; set; }

        /// <summary>
        /// Серия товара (СерияТов).
        /// </summary>
        public string? Series { get; set; }

        /// <summary>
        /// Артикул товара (АртикулТов).
        /// </summary>
        public string? Article { get; set; }

        /// <summary>
        /// Код товара (КодТов).
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Глобальный идентификационный номер товара GTIN (ГТИН).
        /// </summary>
        public string? GlobalTradeItemNumber { get; set; }

        /// <summary>
        /// Код каталога, для государственных и муниципальных закупок (КодКат).
        /// </summary>
        public string? CatalogCode { get; set; }

        /// <summary>
        /// Код вида товара по ТН ВЭД, графа 1б счета-фактуры (КодВидТов).
        /// </summary>
        public string? FeaccCode { get; set; }

        /// <summary>
        /// Код вида продукции, для подакцизных нефтепродуктов (КодВидПр).
        /// </summary>
        public string? ProductTypeCode { get; set; }

        /// <summary>
        /// Код товара по ОКПД 2 (КодТовОКДП2).
        /// </summary>
        public string? Okdp2ProductCode { get; set; }

        /// <summary>
        /// Дополнительная информация к продукции по видам операций (ДопИнфПВидО).
        /// </summary>
        public string? AdditionalOperationInfo { get; set; }

        /// <summary>
        /// Краткие наименования стран происхождения товара, графа 10а счета-фактуры (КрНаимСтрПр).
        /// </summary>
        public List<string>? CountryNames { get; set; }

        /// <summary>
        /// Сопроводительные документы по товару (СопрДокТов).
        /// </summary>
        public List<EdoDocumentDetails>? SupportingDocumentList { get; set; }

        /// <summary>
        /// Данные налогового учета для расчета амортизации по передаваемому объекту (НалУчАморт).
        /// </summary>
        public EdoTaxAccountingAmortization? TaxAccountingAmortization { get; set; }

        /// <summary>
        /// Сумма налога, восстановленного при передаче имущества в уставный капитал (СумНалВосст).
        /// </summary>
        public EdoVatAmount? RecoveredTaxAmount { get; set; }

        /// <summary>
        /// Номера средств идентификации товаров: коды маркировки и упаковок (НомСредИдентТов).
        /// </summary>
        public List<EdoInvoiceItemIdentificationNumber>? ItemIdentificationNumberList { get; set; }

        /// <summary>
        /// Дополнительные сведения о товаре, подлежащем учету в государственной информационной системе (СвГосСист).
        /// </summary>
        public List<EdoGovernmentSystemAdditionalInfo>? GovernmentSystemAdditionalInfoList { get; set; }
    }
}
