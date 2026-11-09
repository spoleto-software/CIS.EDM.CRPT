using System.Text.Json.Serialization;

namespace Spoleto.TrueApi.Edo.Models
{
    /// <summary>
    /// Участник обмена «ЭДО Лайт»: отправитель или получатель документов.
    /// </summary>
    public record EdoParticipant
    {
        /// <summary>
        /// Идентификатор участника в системе ЭДО Оператора.
        /// </summary>
        [JsonPropertyName("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? Id { get; set; }

        /// <summary>
        /// UUID участника.
        /// </summary>
        [JsonPropertyName("extra_id")]
        public string? ExtraId { get; set; }

        /// <summary>
        /// Тип организации: 0 - юридическое лицо, 1 - индивидуальный предприниматель, 2 - нерезидент.
        /// </summary>
        [JsonPropertyName("participant_type")]
        public int? ParticipantType { get; set; }

        /// <summary>
        /// ИНН.
        /// </summary>
        [JsonPropertyName("inn")]
        public string? Inn { get; set; }

        /// <summary>
        /// КПП.
        /// </summary>
        [JsonPropertyName("kpp")]
        public string? Kpp { get; set; }

        /// <summary>
        /// ОГРН.
        /// </summary>
        [JsonPropertyName("ogrn")]
        public string? Ogrn { get; set; }

        /// <summary>
        /// Наименование.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// ФИО.
        /// </summary>
        [JsonPropertyName("fio")]
        public EdoPersonName? Fio { get; set; }

        /// <summary>
        /// Адрес.
        /// </summary>
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        /// <summary>
        /// Юридический адрес строкой.
        /// </summary>
        [JsonPropertyName("legal_address")]
        public string? LegalAddress { get; set; }

        /// <summary>
        /// Статус регистрации в ФНС, например, REGISTERED.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        public override string? ToString() => Name;
    }
}
