using System.Text.Json.Serialization;

namespace CIS.EDM.CRPT.Models
{
    /// <summary>
    /// Результат отправки УПД
    /// </summary>
    public record StringResult
    {
        /// <summary>
        /// Уникальный идентификатор события создания файла информации продавца
        /// </summary>
        /// <remarks>
        /// По данному идентификатору можно производить с документом необходимые действия (редактирование, подписание и т.д.)
        /// </remarks>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        public override string ToString() => Id;
    }
}
