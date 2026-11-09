using System.Text.Json.Serialization;
using Spoleto.TrueApi.Edo.Converters;
using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models
{
    /// <summary>
    /// Цепочка документов «ЭДО Лайт»: документ с исправлениями, корректировками и квитанциями.
    /// </summary>
    /// <remarks>
    /// Поля без вложенного списка описывают последний документ цепочки.
    /// </remarks>
    public record EdoDocumentGroup
    {
        /// <summary>
        /// Идентификатор последнего документа цепочки.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        /// <summary>
        /// Дата создания последнего документа цепочки.
        /// </summary>
        [JsonPropertyName("created_at")]
        [JsonConverter(typeof(UnixDateTimeJsonConverter))]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Дата последнего документа цепочки.
        /// </summary>
        [JsonPropertyName("date")]
        [JsonConverter(typeof(UnixDateTimeJsonConverter))]
        public DateTime? Date { get; set; }

        /// <summary>
        /// Документы цепочки.
        /// </summary>
        [JsonPropertyName("documents")]
        public List<EdoDocumentInfo> Documents { get; set; } = [];

        /// <summary>
        /// Идентификатор группы цепочки документов.
        /// </summary>
        [JsonPropertyName("group_id")]
        public string? GroupId { get; set; }

        /// <summary>
        /// Номер последнего документа цепочки.
        /// </summary>
        [JsonPropertyName("number")]
        public string? Number { get; set; }

        /// <summary>
        /// Отправитель документов.
        /// </summary>
        /// <remarks>
        /// В описании метода приведён только пример исходящих документов с <see cref="Recipient"/>.
        /// </remarks>
        [JsonPropertyName("sender")]
        public EdoParticipant? Sender { get; set; }

        /// <summary>
        /// Получатель документов.
        /// </summary>
        [JsonPropertyName("recipient")]
        public EdoParticipant? Recipient { get; set; }

        /// <summary>
        /// Статус последнего документа цепочки.
        /// </summary>
        [JsonPropertyName("status")]
        public EdoDocumentStatus Status { get; set; }

        /// <summary>
        /// Общая сумма с НДС.
        /// </summary>
        [JsonPropertyName("total_price")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal? TotalPrice { get; set; }

        /// <summary>
        /// Общая сумма НДС.
        /// </summary>
        [JsonPropertyName("total_vat_amount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal? TotalVatAmount { get; set; }

        /// <summary>
        /// Тип последнего документа цепочки.
        /// </summary>
        [JsonPropertyName("type")]
        public EdoDocumentType Type { get; set; }

        /// <summary>
        /// Дата создания документа.
        /// </summary>
        [JsonPropertyName("create_time_stamp")]
        [JsonConverter(typeof(UnixDateTimeJsonConverter))]
        public DateTime? CreateTimestamp { get; set; }

        /// <summary>
        /// Дата обработки документа.
        /// </summary>
        [JsonPropertyName("export_time_stamp")]
        [JsonConverter(typeof(UnixDateTimeJsonConverter))]
        public DateTime? ExportTimestamp { get; set; }

        /// <summary>
        /// Код товарной группы.
        /// </summary>
        [JsonPropertyName("product_group")]
        public int? ProductGroup { get; set; }

        /// <summary>
        /// Тип поведения документа.
        /// </summary>
        [JsonPropertyName("active_type")]
        public int? ActiveType { get; set; }

        public override string? ToString() => Number;
    }
}
