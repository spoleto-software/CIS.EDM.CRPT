using System.Text.Json.Serialization;

namespace Spoleto.TrueApi.Edo.Models
{
    /// <summary>
    /// Ответ «ЭДО Лайт» с идентификатором созданного документа или события.
    /// </summary>
    internal record EdoIdResult
    {
        /// <summary>
        /// Идентификатор документа.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Идентификатор события. Так описание True API называет ответ на загрузку титула покупателя, но тестовый стенд возвращает <see cref="Id"/>.
        /// </summary>
        [JsonPropertyName("event_id")]
        public string? EventId { get; set; }
    }
}
