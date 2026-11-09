using System.Text.Json.Serialization;
using Spoleto.Common.JsonConverters;
using Spoleto.TrueApi.Edo.Converters;
using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models
{
    /// <summary>
    /// Критерии отбора в методе получения списка документов «ЭДО Лайт».
    /// </summary>
    /// <remarks>
    /// Передаётся строкой запроса, поэтому перечисления пишутся числами.
    /// </remarks>
    public record EdoDocumentSearchModel
    {
        /// <summary>
        /// Количество возвращаемых документов. По умолчанию: 10.
        /// </summary>
        [JsonPropertyName("limit")]
        public int? Limit { get; set; }

        /// <summary>
        /// Позиция смещения в наборе результатов. По умолчанию: 0.
        /// </summary>
        [JsonPropertyName("offset")]
        public int? Offset { get; set; }

        /// <summary>
        /// Документы, созданные не раньше этого времени.
        /// </summary>
        [JsonPropertyName("created_from")]
        [JsonConverter(typeof(UnixDateTimeJsonConverter))]
        public DateTime? CreatedFrom { get; set; }

        /// <summary>
        /// Документы, созданные не позже этого времени.
        /// </summary>
        [JsonPropertyName("created_to")]
        [JsonConverter(typeof(UnixDateTimeJsonConverter))]
        public DateTime? CreatedTo { get; set; }

        /// <summary>
        /// ИНН контрагента: для входящего документа - отправитель, для исходящего - получатель.
        /// </summary>
        [JsonPropertyName("partner_inn")]
        public string? PartnerInn { get; set; }

        /// <summary>
        /// Идентификатор получателя документа.
        /// </summary>
        [JsonPropertyName("partner_id")]
        public string? PartnerId { get; set; }

        /// <summary>
        /// Статус документов.
        /// </summary>
        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonIntEnumConverter<EdoDocumentStatus>))]
        public EdoDocumentStatus? Status { get; set; }

        /// <summary>
        /// Тип документов.
        /// </summary>
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonIntEnumConverter<EdoDocumentType>))]
        public EdoDocumentType? Type { get; set; }

        /// <summary>
        /// Папка с документами. Не указывается для поиска по всем папкам.
        /// </summary>
        [JsonPropertyName("folder")]
        [JsonConverter(typeof(JsonIntEnumConverter<EdoFolder>))]
        public EdoFolder? Folder { get; set; }

        /// <summary>
        /// Поле сортировки.
        /// </summary>
        [JsonPropertyName("sortBy")]
        [JsonConverter(typeof(JsonDescriptionEnumConverter<EdoDocumentSortField>))]
        public EdoDocumentSortField? SortBy { get; set; }

        /// <summary>
        /// Сортировка по возрастанию. По умолчанию - по убыванию.
        /// </summary>
        [JsonPropertyName("asc")]
        public bool? Asc { get; set; }

        /// <summary>
        /// Код товарной группы, см. справочник «Список поддерживаемых товарных групп».
        /// </summary>
        [JsonPropertyName("product_group")]
        public int? ProductGroup { get; set; }
    }
}
