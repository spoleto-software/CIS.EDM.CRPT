namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Элемент адреса по ГАР с типом (видом) и наименованием: населенный пункт, элемент планировочной структуры, элемент улично-дорожной сети.
    /// </summary>
    public record EdoAddressElement
    {
        /// <summary>
        /// Тип или вид элемента (Тип, Вид).
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// Наименование (Наим).
        /// </summary>
        public required string Name { get; set; }

        public override string ToString() => $"{Type} {Name}";
    }
}
