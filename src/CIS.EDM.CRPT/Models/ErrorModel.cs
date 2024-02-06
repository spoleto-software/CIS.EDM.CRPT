using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CIS.EDM.CRPT.Models
{
    /// <summary>
    /// Ответ сервиса с ошибкой.
    /// </summary>
    internal class ErrorModel
    {
        /// <summary>
        /// Код ошибки.
        /// </summary>
        [JsonPropertyName("code")]
        public string ErrorCode { get; set; }

        /// <summary>
        /// Текст ошибки.
        /// </summary>
        [JsonPropertyName("error_message")]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Описание ошибки.
        /// </summary>
        [JsonPropertyName("description")]
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Ошибки.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<ErrorInfo> Errors { get; set; }

        public override string ToString() => ErrorMessage ?? String.Join("; ", Errors);
    }
}
