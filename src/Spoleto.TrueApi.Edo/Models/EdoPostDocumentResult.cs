namespace Spoleto.TrueApi.Edo.Models
{
    /// <summary>
    /// Результат загрузки файла информации продавца или покупателя в «ЭДО Лайт».
    /// </summary>
    public record EdoPostDocumentResult
    {
        /// <summary>
        /// Идентификатор, который вернул «ЭДО Лайт».
        /// </summary>
        /// <remarks>
        /// Для информации продавца - идентификатор документа: по нему черновик подписывается и отправляется, а документ скачивается.
        /// Если файл с таким именем уже загружен, сервис возвращает идентификатор ранее загруженного файла.
        /// <para>
        /// Для информации покупателя - идентификатор, который сервис вернул на загрузку титула (в описании метода 9.2.9 он назван event_id).
        /// Входящий документ остаётся под прежним идентификатором.
        /// </para>
        /// </remarks>
        public required string Id { get; set; }

        /// <summary>
        /// Идентификатор файла (ИдФайл), он же имя файла без расширения.
        /// </summary>
        public required string FileId { get; set; }

        /// <summary>
        /// XML отправленного файла.
        /// </summary>
        public required string Content { get; set; }

        /// <summary>
        /// Отправленный файл байт в байт, в windows-1251. Подпись документа делается по этим байтам.
        /// </summary>
        public required byte[] RawContent { get; set; }

        public override string ToString() => $"{Id} ({FileId})";
    }
}
