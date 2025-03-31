using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CIS.EDM.CRPT.Models
{
    /// <summary>
    ///  Пакет успешного ответа на POST запрос получения токена.
    /// </summary>
    public record TokenModel
    {
        /// <summary>
        /// Аутентификационный токен.
        /// </summary>
        [JsonPropertyName("token")]
        [Required]
        public string Token { get; set; }

        /// <summary>
        /// Ключевое слово, которое используется перед токеном.
        /// </summary>
        [JsonIgnore]
        public string Type => "Bearer";

        public override string ToString() => Type + ": " + Token;
    }
}
