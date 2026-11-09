namespace Spoleto.TrueApi.Edo.Exceptions
{
    /// <summary>
    /// Ошибка подписи или загрузки сформированного файла информации продавца или покупателя в «ЭДО Лайт».
    /// </summary>
    /// <remarks>
    /// Несёт файл, который не удалось отправить: по нему видно, что именно не принял сервис.
    /// Текст ошибки - из <see cref="Exception.InnerException"/>, там же исходное исключение, например, <see cref="HttpRequestException"/> с кодом ответа.
    /// </remarks>
    [Serializable]
    public class EdoPostDocumentException : Exception
    {
        public EdoPostDocumentException(string fileId, string content, byte[] rawContent, Exception innerException)
            : base(innerException.Message, innerException)
        {
            FileId = fileId;
            Content = content;
            RawContent = rawContent;
        }

        /// <summary>
        /// Идентификатор файла (ИдФайл), он же имя файла без расширения.
        /// </summary>
        public string FileId { get; }

        /// <summary>
        /// XML файла, который не удалось отправить.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Файл байт в байт, в windows-1251.
        /// </summary>
        public byte[] RawContent { get; }
    }
}
