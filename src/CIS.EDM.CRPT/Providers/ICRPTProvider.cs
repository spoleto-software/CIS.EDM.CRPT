using System.Threading.Tasks;
using CIS.EDM.CRPT.Models;
using CIS.EDM.Providers;

namespace CIS.EDM.CRPT.Providers
{
    /// <summary>
    /// Провайдер для работы с ЭДО от ЦРПТ.
    /// </summary>
    public interface ICRPTProvider : IEdmProvider<CRPTOption>
    {
        /// <summary>
        /// Подписание исходящего документа
        /// </summary>
        /// <param name="settings">Настройки для API</param>
        /// <param name="documentId">Идентификатор документа</param>
        Task SignOutgoingDocumentAsync(CRPTOption settings, string documentId);
    }
}