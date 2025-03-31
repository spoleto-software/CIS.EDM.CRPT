using System.IO.Compression;

namespace CIS.EDM.CRPT.Models
{
    /// <summary>
    /// Zip файл с именем.
    /// </summary>
    public record ZipArchiveInfo
    {
        /// <summary>
        /// Zip файл.
        /// </summary>
        public ZipArchive ZipArchive { get; set; }

        /// <summary>
        /// Имя файла.
        /// </summary>
        public string FileName { get; set; }
    }
}
