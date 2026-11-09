namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Элемент адреса по ГАР с типом и номером: здание, помещение.
    /// </summary>
    public record EdoAddressElementNumber
    {
        /// <summary>
        /// Тип (Тип).
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Номер (Номер).
        /// </summary>
        public required string Number { get; set; }

        public override string ToString() => $"{Type} {Number}";
    }
}
