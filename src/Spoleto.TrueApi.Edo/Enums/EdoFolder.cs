namespace Spoleto.TrueApi.Edo.Enums
{
    /// <summary>
    /// Папка, в которой находятся документы «ЭДО Лайт».
    /// </summary>
    public enum EdoFolder
    {
        /// <summary>
        /// Документы.
        /// </summary>
        Documents = 0,

        /// <summary>
        /// Архив.
        /// </summary>
        Archive = 1,

        /// <summary>
        /// Корзина.
        /// </summary>
        Trash = 2,

        /// <summary>
        /// На подписание.
        /// </summary>
        ToSign = 3,

        /// <summary>
        /// Отклонённые.
        /// </summary>
        Declined = 4,

        /// <summary>
        /// На подпись РВ.
        /// </summary>
        ToSignRV = 5,

        /// <summary>
        /// На подпись РЭ.
        /// </summary>
        ToSignRE = 6,

        /// <summary>
        /// Биллинг.
        /// </summary>
        Billing = 7,

        /// <summary>
        /// Автоподписание.
        /// </summary>
        AutoSigning = 8
    }
}
