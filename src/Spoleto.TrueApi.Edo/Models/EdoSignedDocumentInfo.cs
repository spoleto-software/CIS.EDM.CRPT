namespace Spoleto.TrueApi.Edo.Models
{
    /// <summary>
    /// Файл документа «ЭДО Лайт» и его открепленная подпись.
    /// </summary>
    public record EdoSignedDocumentInfo
    {
        /// <summary>
        /// Имя файла документа без расширения, совпадает с атрибутом ИдФайл в XML.
        /// </summary>
        public required string FileName { get; set; }

        /// <summary>
        /// XML документа, прочитанный в кодировке из своего заголовка (для УПД - windows-1251).
        /// </summary>
        public required string Content { get; set; }

        /// <summary>
        /// Файл документа как есть, байт в байт. Подпись <see cref="Signature"/> сделана по этим байтам.
        /// </summary>
        public required byte[] RawContent { get; set; }

        /// <summary>
        /// Открепленная подпись документа как есть: CMS из файла .p7s, без Base64.
        /// </summary>
        public required byte[] Signature { get; set; }

        public override string ToString() => FileName;
    }
}
