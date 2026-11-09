namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Реквизиты документа (РеквДокТип): основание передачи, документ об отгрузке, доверенность и т. п.
    /// </summary>
    public record EdoDocumentDetails
    {
        /// <summary>
        /// Наименование документа (РеквНаимДок).
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Номер документа (РеквНомерДок). При отсутствии номера - «Без номера».
        /// </summary>
        public required string Number { get; set; }

        /// <summary>
        /// Дата документа (РеквДатаДок).
        /// </summary>
        public required DateTime Date { get; set; }

        /// <summary>
        /// Идентификатор файла обмена документа, подписанного первой стороной (РеквИдФайлДок).
        /// </summary>
        public string? FileId { get; set; }

        /// <summary>
        /// Идентификатор документа, в том числе регистрационный номер в государственной информационной системе (РеквИдДок).
        /// </summary>
        public string? DocumentId { get; set; }

        /// <summary>
        /// Информация об информационной системе, в которой хранится документ (РИдСистХранД).
        /// </summary>
        public string? StorageSystemId { get; set; }

        /// <summary>
        /// URL информационной системы, через которую можно получить сведения о документе (РеквУРЛСистДок).
        /// </summary>
        public string? SystemUrl { get; set; }

        /// <summary>
        /// Дополнительные сведения (РеквДопСведДок).
        /// </summary>
        public string? OtherInfo { get; set; }

        /// <summary>
        /// Идентифицирующие реквизиты экономических субъектов, составивших документ (РеквИдРекСост).
        /// </summary>
        public List<EdoDocumentCreator>? Creators { get; set; }

        public override string ToString() => $"{Name} № {Number} от {Date:dd.MM.yyyy}";
    }
}
