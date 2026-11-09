namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Идентификационные сведения участника (ИдСв). Заполняется ровно одно свойство.
    /// </summary>
    public record EdoOrganizationIdentificationInfo
    {
        /// <summary>
        /// Сведения об индивидуальном предпринимателе (СвИП).
        /// </summary>
        public EdoIndividualEntrepreneur? IndividualEntrepreneur { get; set; }

        /// <summary>
        /// Сведения о юридическом лице, состоящем на учете в налоговых органах (СвЮЛУч).
        /// </summary>
        public EdoLegalPerson? LegalPerson { get; set; }

        /// <summary>
        /// Сведения об иностранном лице, не состоящем на учете в налоговых органах (СвИнНеУч).
        /// </summary>
        public EdoForeignEntity? ForeignPerson { get; set; }

        /// <summary>
        /// Сведения о физическом лице (СвФЛУч), для функции ДОП.
        /// </summary>
        public EdoPhysicalPerson? PhysicalPerson { get; set; }

        public override string ToString() => LegalPerson?.ToString() ?? IndividualEntrepreneur?.ToString() ?? PhysicalPerson?.ToString() ?? ForeignPerson?.ToString() ?? string.Empty;
    }
}
