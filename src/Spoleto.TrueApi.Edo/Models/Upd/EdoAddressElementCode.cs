namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Элемент адреса по ГАР с кодом вида и наименованием: муниципальный район, поселение.
    /// </summary>
    public record EdoAddressElementCode
    {
        /// <summary>
        /// Код вида (ВидКод).
        /// </summary>
        public required string TypeCode { get; set; }

        /// <summary>
        /// Наименование (Наим).
        /// </summary>
        public required string Name { get; set; }

        public override string ToString() => Name;
    }
}
