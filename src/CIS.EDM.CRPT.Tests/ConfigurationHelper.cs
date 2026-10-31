using Microsoft.Extensions.Configuration;

namespace CIS.EDM.CRPT.Tests
{
    /// <summary>
    /// Настройки стенда: appsettings.json, поверх него - секреты пользователя.
    /// </summary>
    /// <remarks>
    /// UserSecretsId в проекте тот же, что у Spoleto.TrueApi.Tests: если стенд там уже настроен, здесь ничего настраивать не нужно.
    /// </remarks>
    internal static class ConfigurationHelper
    {
        private static readonly IConfigurationRoot _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<TestOption>()
            .Build();

        public static TestOption GetTestOption() => _config.GetSection(nameof(TestOption)).Get<TestOption>();
    }
}
