namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Адрес по уникальному номеру объекта адресации в государственном адресном реестре (АдрГАР).
    /// </summary>
    public record EdoAddressCode
    {
        /// <summary>
        /// Уникальный номер адреса в ГАР (ИдНом).
        /// </summary>
        public required Guid UniqueCode { get; set; }

        /// <summary>
        /// Индекс (Индекс).
        /// </summary>
        public string? ZipCode { get; set; }

        /// <summary>
        /// Код субъекта Российской Федерации (Регион).
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// Наименование субъекта Российской Федерации (НаимРегион).
        /// </summary>
        public string? RegionName { get; set; }

        /// <summary>
        /// Муниципальный район, муниципальный округ, городской округ или внутригородская территория (МуниципРайон).
        /// </summary>
        public EdoAddressElementCode? MunicipalDistrict { get; set; }

        /// <summary>
        /// Городское поселение, сельское поселение или межселенная территория (ГородСелПоселен).
        /// </summary>
        public EdoAddressElementCode? UrbanRuralSettlement { get; set; }

        /// <summary>
        /// Населенный пункт (НаселенПункт): вид и наименование.
        /// </summary>
        public EdoAddressElement? Settlement { get; set; }

        /// <summary>
        /// Элемент планировочной структуры (ЭлПланСтруктур): тип и наименование.
        /// </summary>
        public EdoAddressElement? PlanningStructureElement { get; set; }

        /// <summary>
        /// Элемент улично-дорожной сети (ЭлУлДорСети): тип и наименование.
        /// </summary>
        public EdoAddressElement? StreetRoadNetworkElement { get; set; }

        /// <summary>
        /// Номер земельного участка (ЗемелУчасток).
        /// </summary>
        public string? LandPlotNumber { get; set; }

        /// <summary>
        /// Здания, сооружения, объекты незавершенного строительства (Здание).
        /// </summary>
        public List<EdoAddressElementNumber>? BuildingList { get; set; }

        /// <summary>
        /// Помещение в пределах здания (ПомещЗдания).
        /// </summary>
        public EdoAddressElementNumber? BuildingRoom { get; set; }

        /// <summary>
        /// Помещение в пределах квартиры (ПомещКвартиры).
        /// </summary>
        public EdoAddressElementNumber? ApartmentRoom { get; set; }

        public override string ToString() => UniqueCode.ToString();
    }
}
