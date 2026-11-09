using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Код содержания операции (КодСодОпер).
    /// </summary>
    public record EdoOperationNameInfo
    {
        /// <summary>
        /// Итог приемки товаров (работ, услуг, прав) (КодИтога).
        /// </summary>
        public EdoOperationNameCode Code { get; set; } = EdoOperationNameCode.Accepted;

        /// <summary>
        /// Реквизиты документа, оформляющего расхождения (РеквДокРасх).
        /// </summary>
        public EdoDocumentDetails? DiscrepancyDocument { get; set; }
    }
}
