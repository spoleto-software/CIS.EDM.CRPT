using System.ComponentModel;

namespace Spoleto.TrueApi.Edo.Enums
{
    /// <summary>
    /// Поле сортировки списка документов «ЭДО Лайт».
    /// </summary>
    public enum EdoDocumentSortField
    {
        /// <summary>
        /// Время создания документа.
        /// </summary>
        [Description("created_at")]
        CreatedAt
    }
}
