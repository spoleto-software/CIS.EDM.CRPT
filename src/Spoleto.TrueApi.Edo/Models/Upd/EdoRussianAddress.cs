namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Адрес в Российской Федерации (АдрРФ).
    /// </summary>
    public record EdoRussianAddress
    {
        /// <summary>
        /// Индекс (Индекс).
        /// </summary>
        public string? ZipCode { get; set; }

        /// <summary>
        /// Код субъекта Российской Федерации (КодРегион).
        /// </summary>
        public required string RegionCode { get; set; }

        /// <summary>
        /// Наименование субъекта Российской Федерации (НаимРегион).
        /// </summary>
        public required string Region { get; set; }

        /// <summary>
        /// Район (Район).
        /// </summary>
        public string? Territory { get; set; }

        /// <summary>
        /// Город (Город).
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Населенный пункт (НаселПункт).
        /// </summary>
        public string? Locality { get; set; }

        /// <summary>
        /// Улица (Улица).
        /// </summary>
        public string? Street { get; set; }

        /// <summary>
        /// Дом (Дом).
        /// </summary>
        public string? Building { get; set; }

        /// <summary>
        /// Корпус (Корпус).
        /// </summary>
        public string? Block { get; set; }

        /// <summary>
        /// Квартира (Кварт).
        /// </summary>
        public string? Apartment { get; set; }

        /// <summary>
        /// Иные сведения об адресе (ИныеСвед).
        /// </summary>
        public string? OtherInfo { get; set; }

        public override string ToString() => string.Join(", ", new[] { ZipCode, Region, City, Locality, Street, Building, Block, Apartment }.Where(x => !string.IsNullOrEmpty(x)));
    }
}
