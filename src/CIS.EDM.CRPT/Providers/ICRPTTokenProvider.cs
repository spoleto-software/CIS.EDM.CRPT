using CIS.EDM.CRPT.Models;
using System.Threading.Tasks;

namespace CIS.EDM.CRPT.Providers
{
    public interface ICRPTTokenProvider
    {
        /// <summary>
        /// Метод для получения токена.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <returns>Токена доступа.</returns>
        TokenModel GetToken(CRPTOption settings)
            => GetTokenAsync(settings).GetAwaiter().GetResult();

        /// <summary>
        /// Метод для получения токена.
        /// </summary>
        /// <param name="settings">Настройки провайдера.</param>
        /// <returns>Токена доступа.</returns>
        Task<TokenModel> GetTokenAsync(CRPTOption settings);
    }
}
