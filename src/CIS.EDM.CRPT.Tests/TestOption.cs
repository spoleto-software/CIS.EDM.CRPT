namespace CIS.EDM.CRPT.Tests
{
    /// <summary>
    /// Настройки стенда для <see cref="TrueApiCRPTProviderStandTests"/>: те же, что у тестов Spoleto.TrueApi.
    /// </summary>
    public record TestOption
    {
        /// <summary>
        /// Отпечаток сертификата из хранилища, регистр не важен.
        /// </summary>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Адрес True API, например, https://markirovka.sandbox.crptech.ru. Провайдер берёт его из <see cref="Models.CRPTOption.AuthUrl"/>.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// ИНН владельца сертификата.
        /// </summary>
        public string Inn { get; set; }
    }
}
