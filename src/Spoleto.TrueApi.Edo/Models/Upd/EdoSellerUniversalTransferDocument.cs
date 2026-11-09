using Spoleto.TrueApi.Edo.Enums;

namespace Spoleto.TrueApi.Edo.Models.Upd
{
    /// <summary>
    /// Информация продавца УПД / УПДи по формату приказа ФНС России от 19.12.2023 № ЕД-7-26/970@, версия формата 5.03 (Файл).
    /// </summary>
    /// <remarks>
    /// Код КНД (1115131) и версия формата (5.03) - константы файла и в модель не входят.
    /// Сведений о прослеживаемости нет: «ЭДО Лайт» не принимает УПД с прослеживаемыми товарами.
    /// </remarks>
    public record EdoSellerUniversalTransferDocument
    {
        /// <summary>
        /// Идентификатор файла (ИдФайл), он же имя файла без расширения.
        /// </summary>
        /// <remarks>
        /// Если не задан, формируется при отправке: ON_NSCHFDOPPR_{получатель}_{отправитель}_{ГГГГММДД}_{GUID}_0_N3_N4_N5_N6_00,
        /// где N3-N6 - <see cref="GoodsCategories"/>.
        /// </remarks>
        public string? FileId { get; set; }

        /// <summary>
        /// Версия программы, с помощью которой сформирован файл (ВерсПрог).
        /// </summary>
        public string ApplicationCreator { get; set; } = "Spoleto.TrueApi.Edo";

        /// <summary>
        /// Идентификатор участника ЭДО отправителя вместе с кодом оператора, например, 2LT-0000000001. Входит в имя файла.
        /// </summary>
        public required string SenderEdoId { get; set; }

        /// <summary>
        /// Идентификатор участника ЭДО получателя вместе с кодом оператора. Входит в имя файла, по нему документ доставляется получателю.
        /// </summary>
        public required string RecipientEdoId { get; set; }

        /// <summary>
        /// Дата и время формирования файла (ДатаИнфПр, ВремИнфПр), UTC.
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Категории товаров для признаков N3-N6 в имени файла.
        /// </summary>
        /// <remarks>
        /// Если не задано: маркировка, если у строк есть средства идентификации (<see cref="EdoInvoiceItemAdditionalInfo.ItemIdentificationNumberList"/>).
        /// </remarks>
        public List<EdoGoodsCategory>? GoodsCategories { get; set; }

        #region Документ

        /// <summary>
        /// Функция документа (Функция).
        /// </summary>
        public required EdoDocumentFunction Function { get; set; }

        /// <summary>
        /// Номер документа (НомерДок).
        /// </summary>
        public required string DocumentNumber { get; set; }

        /// <summary>
        /// Дата документа (ДатаДок).
        /// </summary>
        public required DateTime DocumentDate { get; set; }

        /// <summary>
        /// Уникальный идентификатор документа, выдаваемый государственной информационной системой (УИД).
        /// </summary>
        public string? DocumentUid { get; set; }

        /// <summary>
        /// Наименование первичного документа, определенное организацией (НаимДокОпр).
        /// </summary>
        /// <remarks>
        /// Если не задано, берется по функции: для СЧФДОП и ДОП - типовое наименование, для СЧФ не формируется.
        /// </remarks>
        public string? DocumentName { get; set; }

        /// <summary>
        /// Наименование документа по факту хозяйственной жизни (ПоФактХЖ).
        /// </summary>
        /// <remarks>
        /// Если не задано, берется по функции, для СЧФ не формируется.
        /// </remarks>
        public string? DocumentEconomicName { get; set; }

        /// <summary>
        /// Информация о наличии согласованной структуры дополнительных информационных полей (СоглСтрДопИнф) в виде XXXX.YYYY.NNNN.
        /// </summary>
        public string? ApprovedStructureAdditionalInfoFields { get; set; }

        /// <summary>
        /// Экономический субъект - составитель файла (НаимЭконСубСост): в файл попадает его наименование с ИНН и КПП.
        /// </summary>
        public required EdoOrganization DocumentCreator { get; set; }

        /// <summary>
        /// Основание, по которому экономический субъект является составителем файла (ОснДоверОргСост).
        /// </summary>
        /// <remarks>
        /// Обязательно, если составитель не продавец.
        /// </remarks>
        public EdoDocumentDetails? DocumentCreatorBase { get; set; }

        #endregion

        #region Сведения о счете-фактуре (СвСчФакт)

        /// <summary>
        /// Номер исправления (ИспрДок/НомИспр), от 1. Если задан, документ отправляется как УПДи.
        /// </summary>
        public string? RevisionNumber { get; set; }

        /// <summary>
        /// Дата исправления (ИспрДок/ДатаИспр). Обязательна при <see cref="RevisionNumber"/>.
        /// </summary>
        public DateTime? RevisionDate { get; set; }

        /// <summary>
        /// Имя файла информации продавца исправляемого документа (ИмяФайлИспрПрод).
        /// </summary>
        public string? CorrectedSellerFileName { get; set; }

        /// <summary>
        /// Имя файла информации покупателя исправляемого документа (ИмяФайлИспрПок).
        /// </summary>
        public string? CorrectedBuyerFileName { get; set; }

        /// <summary>
        /// Сведения о продавце, строки 2, 2а, 2б счета-фактуры (СвПрод).
        /// </summary>
        public required List<EdoOrganization> Sellers { get; set; }

        /// <summary>
        /// Грузоотправители, строка 3 счета-фактуры (ГрузОт/ГрузОтпр).
        /// </summary>
        public List<EdoOrganization>? Shippers { get; set; }

        /// <summary>
        /// Грузополучатели, строка 4 счета-фактуры (ГрузПолуч).
        /// </summary>
        public List<EdoOrganization>? Consignees { get; set; }

        /// <summary>
        /// Сведения о платежно-расчетных документах, строка 5 счета-фактуры (СвПРД).
        /// </summary>
        public List<EdoPaymentDocumentInfo>? PaymentDocumentInfoList { get; set; }

        /// <summary>
        /// Реквизиты документов, подтверждающих отгрузку, строка 5а счета-фактуры (ДокПодтвОтгрНом).
        /// </summary>
        public List<EdoDocumentDetails>? DocumentShipmentList { get; set; }

        /// <summary>
        /// Сведения о покупателе, строки 6, 6а, 6б счета-фактуры (СвПокуп).
        /// </summary>
        public required List<EdoOrganization> Buyers { get; set; }

        /// <summary>
        /// Код валюты по ОКВ, строка 7 счета-фактуры (ДенИзм/КодОКВ).
        /// </summary>
        public string CurrencyCode { get; set; } = "643";

        /// <summary>
        /// Наименование валюты по ОКВ (ДенИзм/НаимОКВ).
        /// </summary>
        public string? CurrencyName { get; set; } = "Российский рубль";

        /// <summary>
        /// Курс валюты (ДенИзм/КурсВал).
        /// </summary>
        public decimal? CurrencyRate { get; set; }

        /// <summary>
        /// Дополнительные сведения об участниках факта хозяйственной жизни, основаниях и обстоятельствах его проведения (ДопСвФХЖ1).
        /// </summary>
        public EdoAdditionalTransactionParticipantInfo? AdditionalTransactionParticipantInfo { get; set; }

        /// <summary>
        /// Информационное поле факта хозяйственной жизни 1 (ИнфПолФХЖ1). Здесь же передаются сведения о МЧД (ТекстИнф Идентиф="МЧД").
        /// </summary>
        public EdoOtherEconomicInfo? OtherEconomicInfo { get; set; }

        #endregion

        /// <summary>
        /// Строки таблицы счета-фактуры (ТаблСчФакт/СведТов).
        /// </summary>
        public required List<EdoInvoiceItem> Items { get; set; }

        /// <summary>
        /// Сведения о факте отгрузки товаров (СвПродПер). Обязательны при функции СЧФДОП и ДОП.
        /// </summary>
        public EdoTransferInfo? TransferInfo { get; set; }

        /// <summary>
        /// Лица, подписывающие файл (Подписант).
        /// </summary>
        public required List<EdoSigner> Signers { get; set; }

        /// <summary>
        /// Признак УПДи: задан номер исправления.
        /// </summary>
        public bool IsUPDi => !string.IsNullOrEmpty(RevisionNumber);

        public override string ToString() => $"УПД № {DocumentNumber} от {DocumentDate:dd.MM.yyyy}";
    }
}
