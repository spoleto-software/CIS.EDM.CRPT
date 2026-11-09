using System.IO.Compression;
using System.Text;
using System.Xml;
using Spoleto.TrueApi.Edo.Models;

namespace Spoleto.TrueApi.Edo.Helpers
{
    /// <summary>
    /// Чтение ZIP-архива документа «ЭДО Лайт» с подписями и квитанциями.
    /// </summary>
    internal static class EdoArchiveHelper
    {
        /// <summary>
        /// Начало имени файла информации продавца УПД и УПДи.
        /// </summary>
        private const string SellerFilePrefix = "ON_NSCHFDOPPR";

        private const string ArchiveExtension = ".zip";
        private const string ContentExtension = ".xml";
        private const string SignatureExtension = ".p7s";

        static EdoArchiveHelper()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//support for "windows-1251"
        }

        /// <summary>
        /// Файл документа и его открепленная подпись из архива.
        /// </summary>
        /// <param name="archive">Архив документа.</param>
        /// <param name="archiveFileName">Имя архива из заголовка Content-Disposition, если сервис его передал.</param>
        public static EdoSignedDocumentInfo ReadSignedDocument(ZipArchive archive, string? archiveFileName)
        {
            var fileName = GetDocumentFileName(archive, archiveFileName);

            var contentEntry = FindEntry(archive, fileName + ContentExtension);
            var signatureEntry = FindEntry(archive, fileName + SignatureExtension);
            if (contentEntry == null || signatureEntry == null)
            {
                throw CreateException(archive, $"Нет файла документа {fileName}{ContentExtension} или его подписи {fileName}{SignatureExtension}");
            }

            var rawContent = ReadAllBytes(contentEntry);

            return new EdoSignedDocumentInfo
            {
                FileName = fileName,
                Content = ReadXml(rawContent),
                RawContent = rawContent,
                Signature = ReadAllBytes(signatureEntry)
            };
        }

        /// <summary>
        /// Кодировку определяет XML-парсер: по BOM или по объявлению &lt;?xml encoding="windows-1251"?&gt;. Текст возвращается как в файле, без BOM.
        /// </summary>
        private static string ReadXml(byte[] content)
        {
            using var xmlReader = new XmlTextReader(new MemoryStream(content)) { DtdProcessing = DtdProcessing.Prohibit };
            xmlReader.MoveToContent();

            using var reader = new StreamReader(new MemoryStream(content), xmlReader.Encoding ?? Encoding.UTF8, true);

            return reader.ReadToEnd();
        }

        /// <summary>
        /// Архив называется по файлу документа: ON_NSCHFDOPPR_….zip содержит ON_NSCHFDOPPR_….xml и ON_NSCHFDOPPR_….p7s.
        /// Если имени архива нет или файла с таким именем в архиве нет, берём единственную информацию продавца УПД:
        /// кроме неё в архиве лежат квитанции и информация покупателя.
        /// </summary>
        private static string GetDocumentFileName(ZipArchive archive, string? archiveFileName)
        {
            if (!string.IsNullOrEmpty(archiveFileName))
            {
                var fileName = archiveFileName.EndsWith(ArchiveExtension, StringComparison.OrdinalIgnoreCase)
                    ? archiveFileName[..^ArchiveExtension.Length]
                    : archiveFileName;

                if (FindEntry(archive, fileName + ContentExtension) != null)
                {
                    return fileName;
                }
            }

            var sellerFileNames = archive.Entries
                .Where(x => x.Name.StartsWith(SellerFilePrefix, StringComparison.Ordinal) && x.Name.EndsWith(ContentExtension, StringComparison.Ordinal))
                .Select(x => x.Name[..^ContentExtension.Length])
                .ToList();

            if (sellerFileNames.Count != 1)
            {
                throw CreateException(archive, $"Ожидалась одна информация продавца УПД {SellerFilePrefix}_…{ContentExtension}, найдено: {sellerFileNames.Count}");
            }

            return sellerFileNames[0];
        }

        private static ZipArchiveEntry? FindEntry(ZipArchive archive, string name)
            => archive.Entries.FirstOrDefault(x => x.Name == name);

        private static byte[] ReadAllBytes(ZipArchiveEntry entry)
        {
            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            entryStream.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }

        private static InvalidDataException CreateException(ZipArchive archive, string message)
            => new($"{message}. Файлы архива: {string.Join(", ", archive.Entries.Select(x => x.FullName))}.");
    }
}
