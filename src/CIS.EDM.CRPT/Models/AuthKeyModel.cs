using System.Text.Json.Serialization;

namespace CIS.EDM.CRPT.Models
{
    /// <summary>
    ///  Пакет успешного ответа на запрос получения предварительного ключа перед запросом токена
    /// </summary>
    internal class AuthKeyModel
    {
        /// <summary>
        /// Уникальный идентификатор сгенерированных случайных данных.
        /// </summary>
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        /// <summary>
        /// Случайная строка данных.
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }
    }
}
