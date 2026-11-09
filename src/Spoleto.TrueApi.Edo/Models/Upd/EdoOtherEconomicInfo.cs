namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Информационное поле факта хозяйственной жизни (ИнфПолФХЖ1, ИнфПолФХЖ3, ИнфПолФХЖ4).
    /// </summary>
    public record EdoOtherEconomicInfo
    {
        /// <summary>
        /// Идентификатор файла информационного поля, GUID (ИдФайлИнфПол).
        /// </summary>
        public string? InfoFileId { get; set; }

        /// <summary>
        /// Текстовая информация (ТекстИнф).
        /// </summary>
        public List<EdoOtherEconomicInfoItem>? Items { get; set; }
    }
}
