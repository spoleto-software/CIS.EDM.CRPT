using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models
{
    /// <summary>
    /// Сведения из файла информации продавца УПД / УПДи, которые повторяются в информации покупателя.
    /// </summary>
    /// <remarks>
    /// Даты и время - строки как в файле: информация покупателя повторяет их без изменений.
    /// </remarks>
    public record EdoSellerDocumentInfo
    {
        /// <summary>
        /// Идентификатор файла информации продавца, атрибут ИдФайл. Совпадает с именем файла без расширения.
        /// </summary>
        public required string FileId { get; set; }

        /// <summary>
        /// Дата формирования файла информации продавца в формате ДД.ММ.ГГГГ, атрибут ДатаИнфПр.
        /// </summary>
        public required string DateCreation { get; set; }

        /// <summary>
        /// Время формирования файла информации продавца в формате ЧЧ.ММ.СС, атрибут ВремИнфПр.
        /// </summary>
        public required string TimeCreation { get; set; }

        /// <summary>
        /// Наименование первичного документа, согласованное сторонами сделки, атрибут НаимДокОпр.
        /// </summary>
        /// <remarks>
        /// У счета-фактуры без документа об отгрузке (Функция = СЧФ) атрибута нет. К такому файлу нет и информации покупателя.
        /// </remarks>
        public string? DocumentName { get; set; }

        /// <summary>
        /// Функция документа, атрибут Функция.
        /// </summary>
        public required EdoDocumentFunction Function { get; set; }

        /// <summary>
        /// Номер счета-фактуры (документа об отгрузке), атрибут НомерДок.
        /// </summary>
        public string? DocumentNumber { get; set; }

        /// <summary>
        /// Дата счета-фактуры (документа об отгрузке) в формате ДД.ММ.ГГГГ, атрибут ДатаДок.
        /// </summary>
        public required string DocumentDate { get; set; }

        /// <summary>
        /// Уникальный идентификатор документа, выданный государственной информационной системой, атрибут УИД.
        /// </summary>
        public string? DocumentUid { get; set; }

        /// <summary>
        /// Документ - исправление (УПДи): в файле есть номер исправления ИспрДок/НомИспр.
        /// </summary>
        public bool IsUPDi { get; set; }

        /// <summary>
        /// Открепленная подпись файла информации продавца как есть: CMS из файла .p7s, без Base64.
        /// </summary>
        public required byte[] Signature { get; set; }

        public override string ToString() => FileId;
    }
}
