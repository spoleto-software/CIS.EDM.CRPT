namespace Spoleto.TrueApi.Edo.Models.Options
{
    /// <summary>
    /// Настройки провайдера методов «ЭДО Лайт» в True API.
    /// </summary>
    public record TrueApiEdoProviderOption
    {
        /// <summary>
        /// Адрес стенда True API без пути, например, https://markirovka.crpt.ru.
        /// </summary>
        /// <remarks>
        /// По этому же адресу запрашивается токен единой аутентификации.
        /// </remarks>
        public required string ServiceUrl { get; set; }

        /// <summary>
        /// Отпечаток сертификата УКЭП, которым подписываются запрос токена и исходящие документы.
        /// </summary>
        public required string CertificateThumbprint { get; set; }

        /// <summary>
        /// ИНН участника оборота товаров, под которым запрашивается авторизация.
        /// </summary>
        public string? Inn { get; set; }
    }
}
