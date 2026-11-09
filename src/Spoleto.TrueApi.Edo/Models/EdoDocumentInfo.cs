using System.Text.Json.Serialization;
using Spoleto.TrueApi.Edo.Converters;
using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models
{
    /// <summary>
    /// Документ «ЭДО Лайт» в цепочке.
    /// </summary>
    public record EdoDocumentInfo
    {
        /// <summary>
        /// Идентификатор документа в системе ЭДО Оператора.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        /// <summary>
        /// Дата создания документа.
        /// </summary>
        [JsonPropertyName("created_at")]
        [JsonConverter(typeof(UnixDateTimeJsonConverter))]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Дата документа.
        /// </summary>
        [JsonPropertyName("date")]
        [JsonConverter(typeof(UnixDateTimeJsonConverter))]
        public DateTime? Date { get; set; }

        /// <summary>
        /// Номер документа.
        /// </summary>
        [JsonPropertyName("number")]
        public string? Number { get; set; }

        /// <summary>
        /// Дата обработки документа.
        /// </summary>
        [JsonPropertyName("processed_at")]
        [JsonConverter(typeof(UnixDateTimeJsonConverter))]
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Содержание атрибута ИспрСчФ.
        /// </summary>
        [JsonPropertyName("revision")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int? Revision { get; set; }

        /// <summary>
        /// Статус документа.
        /// </summary>
        [JsonPropertyName("status")]
        public EdoDocumentStatus Status { get; set; }

        /// <summary>
        /// Цена с НДС.
        /// </summary>
        [JsonPropertyName("total_price")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal? TotalPrice { get; set; }

        /// <summary>
        /// Сумма НДС.
        /// </summary>
        [JsonPropertyName("total_vat_amount")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal? TotalVatAmount { get; set; }

        /// <summary>
        /// Тип документа.
        /// </summary>
        [JsonPropertyName("type")]
        public EdoDocumentType Type { get; set; }

        /// <summary>
        /// Есть подписанный титул покупателя.
        /// </summary>
        [JsonPropertyName("is_title_signed")]
        public bool? IsTitleSigned { get; set; }

        /// <summary>
        /// Сервис, создавший документ: 0 - doc-off, 1 - edo-roaming.
        /// </summary>
        [JsonPropertyName("created_by")]
        public int? CreatedBy { get; set; }

        /// <summary>
        /// Документ создан с помощью «1С:Хаб».
        /// </summary>
        [JsonPropertyName("created_hub")]
        public bool? CreatedHub { get; set; }

        /// <summary>
        /// Документ поступил в составе логического сообщения закрытого типа.
        /// </summary>
        [JsonPropertyName("is_closed_lm_part")]
        public bool? IsClosedLmPart { get; set; }

        /// <summary>
        /// Статус обработки пакета с документами в составе логического сообщения закрытого типа.
        /// </summary>
        [JsonPropertyName("package_status")]
        public int? PackageStatus { get; set; }

        /// <summary>
        /// К документу поступила новая квитанция.
        /// </summary>
        [JsonPropertyName("uni_new_msg")]
        public bool? UniNewMessage { get; set; }

        public override string? ToString() => Number;
    }
}
