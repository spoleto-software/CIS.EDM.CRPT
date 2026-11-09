using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Лицо, подписывающее файл информации продавца или покупателя (Подписант).
    /// </summary>
    public record EdoSigner
    {
        /// <summary>
        /// Должность (Должн).
        /// </summary>
        public string? JobTitle { get; set; }

        /// <summary>
        /// Тип подписи (ТипПодпис).
        /// </summary>
        public EdoSignatureType? SignatureType { get; set; }

        /// <summary>
        /// Дата подписания документа (ДатаПодДок).
        /// </summary>
        public DateTime? SigningDate { get; set; }

        /// <summary>
        /// Способ подтверждения полномочий представителя на подписание документа (СпосПодтПолном).
        /// </summary>
        public required EdoAuthorityConfirmationType AuthorityConfirmationType { get; set; }

        /// <summary>
        /// Дополнительные сведения о подписанте (ДопСведПодп).
        /// </summary>
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Фамилия, имя, отчество (ФИО).
        /// </summary>
        public required EdoPerson Person { get; set; }

        /// <summary>
        /// Сведения о машиночитаемой доверенности (СвДоверЭл).
        /// Обязательны при <see cref="EdoAuthorityConfirmationType.SystemMachineReadablePoAWithDocInfo"/>.
        /// </summary>
        public EdoElectronicPoAInfo? ElectronicPoAInfo { get; set; }

        /// <summary>
        /// Сведения о доверенности на бумажном носителе (СвДоверБум).
        /// Обязательны при <see cref="EdoAuthorityConfirmationType.PaperBasedDocument"/>. Не формируются, если задана <see cref="ElectronicPoAInfo"/>.
        /// </summary>
        public EdoPaperPoAInfo? PaperPoAInfo { get; set; }

        public override string ToString() => Person.ToString();
    }
}
