namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Сведения о доверенности в электронной форме в машиночитаемом виде (СвДоверЭл).
    /// </summary>
    public record EdoElectronicPoAInfo
    {
        /// <summary>
        /// Единый регистрационный номер доверенности (НомДовер).
        /// </summary>
        public required Guid RegistrationNumber { get; set; }

        /// <summary>
        /// Дата выдачи доверенности (ДатаВыдДовер).
        /// </summary>
        public required DateTime IssueDate { get; set; }

        /// <summary>
        /// Внутренний номер доверенности (ВнНомДовер).
        /// </summary>
        public string? InternalNumber { get; set; }

        /// <summary>
        /// Дата внутренней регистрации доверенности (ДатаВнРегДовер).
        /// </summary>
        public DateTime? InternalRegistrationDate { get; set; }

        /// <summary>
        /// Информация об информационной системе, в которой хранится доверенность (ИдСистХран).
        /// </summary>
        public required string SystemIdentification { get; set; }

        /// <summary>
        /// URL информационной системы, через которую можно получить доверенность (УРЛСист).
        /// </summary>
        public string? SystemUrl { get; set; }
    }
}
