using CIS.EDM.Providers;

namespace CIS.EDM.CRPT.Models
{
    /// <summary>
    /// Настройки ЦРПТ провайдера.
    /// </summary>
    public record CRPTOption : IEdmOption
    {
        /// <summary>
        /// Адрес ЦРПТ сервиса.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Адрес единой аутентификации True API.
        /// </summary>
        public string AuthUrl { get; set; }

        /// <summary>
        /// Публичный отпечаток сертификата.
        /// </summary>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Валидация настроек (проверка, что указаны все обязательные параметры).
        /// Если не указан какой-либо из обязательных параметров, то будет сгенерировано исключение типа <see cref="System.ArgumentNullException"/>.
        /// </summary>
        void IEdmOption.Validate()
        {
            if (string.IsNullOrEmpty(AuthUrl))
                throw new System.ArgumentNullException(nameof(AuthUrl));

            if (string.IsNullOrEmpty(ServiceUrl))
                throw new System.ArgumentNullException(nameof(ServiceUrl));
        }
    }
}
