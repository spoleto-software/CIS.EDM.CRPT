using System.Globalization;
using System.Xml.Linq;
using Spoleto.TrueApi.Edo.Models;
using Spoleto.TrueApi.Edo.Models.Upd;

namespace Spoleto.TrueApi.Edo.Helpers
{
    /// <summary>
    /// Формирование файла информации покупателя УПД / УПДи формата 970 (версия 5.03).
    /// </summary>
    /// <remarks>
    /// Порядок элементов повторяет генератор CIS.EDM.CRPT (XmlHelperV5_03.Buyer), кроме двух мест, где тот расходится с форматом:
    /// вид операции пишется в атрибут ВидОпер (а не ВидОперации), а реквизиты документа о расхождениях РеквДокРасх - внутрь КодСодОпер.
    /// </remarks>
    internal static partial class EdoUniversalTransferDocumentXmlHelper
    {
        private const string BuyerFileIdPrefix = "ON_NSCHFDOPPOK";
        private const string BuyerTaxDocumentCode = "1115132";

        /// <summary>
        /// Идентификатор файла: ON_NSCHFDOPPOK_{получатель}_{отправитель}_{ГГГГММДД}_{GUID}_N2_N3_N4_N5_N6_00.
        /// </summary>
        /// <remarks>
        /// Покупатель отвечает продавцу: отправитель и получатель файла продавца меняются местами, признаки N2-N6 повторяются.
        /// </remarks>
        public static string GetFileId(EdoBuyerUniversalTransferDocument document, EdoSellerDocumentInfo sellerDocumentInfo)
        {
            // ON_NSCHFDOPPR_{получатель}_{отправитель}_{ГГГГММДД}_{GUID}_N2_N3_N4_N5_N6_00
            var parts = sellerDocumentInfo.FileId.Split('_');
            if (parts.Length != 12 || $"{parts[0]}_{parts[1]}" != FileIdPrefix)
            {
                throw new InvalidDataException($"Идентификатор файла информации продавца {sellerDocumentInfo.FileId} не соответствует формату 970: ожидался {FileIdPrefix}_{{получатель}}_{{отправитель}}_{{ГГГГММДД}}_{{GUID}}_N2_N3_N4_N5_N6_00.");
            }

            return $"{BuyerFileIdPrefix}_{parts[3]}_{parts[2]}_{document.DateCreation.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}_{Guid.NewGuid():D}_{string.Join("_", parts[6..11])}_00";
        }

        /// <summary>
        /// XML файла информации покупателя с объявлением кодировки windows-1251.
        /// </summary>
        /// <param name="document">Информация покупателя.</param>
        /// <param name="sellerDocumentInfo">Сведения из файла продавца, которые повторяет информация покупателя.</param>
        /// <param name="fileId">Идентификатор файла информации покупателя.</param>
        public static string CreateXml(EdoBuyerUniversalTransferDocument document, EdoSellerDocumentInfo sellerDocumentInfo, string fileId)
        {
            Validate(document, sellerDocumentInfo);

            return CreateFile(fileId, document.ApplicationCreator, CreateDocument(document, sellerDocumentInfo));
        }

        private static void Validate(EdoBuyerUniversalTransferDocument document, EdoSellerDocumentInfo sellerDocumentInfo)
        {
            // НаимДокОпрПр обязателен, а в файле продавца НаимДокОпр есть только у документа об отгрузке.
            if (string.IsNullOrEmpty(sellerDocumentInfo.DocumentName))
                throw new InvalidDataException($"Файл продавца {sellerDocumentInfo.FileId}: нет наименования первичного документа (НаимДокОпр). Информация покупателя к счету-фактуре без документа об отгрузке (Функция = СЧФ) не формируется.");

            if (document.Signers == null || document.Signers.Count == 0)
                throw new ArgumentException("Не указан подписант (Signers).", nameof(document));

            if (document.BuyerInfoCircumPublicProc is { } info && (info.FinancialObligationInfoList == null || info.FinancialObligationInfoList.Count == 0))
                throw new ArgumentException("Не указана информация для сведений о денежном обязательстве (BuyerInfoCircumPublicProc.FinancialObligationInfoList).", nameof(document));
        }

        private static XElement CreateDocument(EdoBuyerUniversalTransferDocument document, EdoSellerDocumentInfo sellerDocumentInfo)
            => new("Документ",
                new XAttribute("КНД", BuyerTaxDocumentCode),
                Attribute("УИД", sellerDocumentInfo.DocumentUid),
                DateAttribute("ДатаИнфПок", document.DateCreation),
                new XAttribute("ВремИнфПок", document.DateCreation.ToString(TimeFormat, CultureInfo.InvariantCulture)),
                new XAttribute("НаимЭконСубСост", GetEconomicEntityName(document.DocumentCreator)),
                // Даты и время файла продавца - строки из его XML, повторяются без изменений.
                new XElement("ИдИнфПрод",
                    new XAttribute("ИдФайлИнфПр", sellerDocumentInfo.FileId),
                    new XAttribute("ДатаФайлИнфПр", sellerDocumentInfo.DateCreation),
                    new XAttribute("ВремФайлИнфПр", sellerDocumentInfo.TimeCreation),
                    new XElement("ЭП", Convert.ToBase64String(sellerDocumentInfo.Signature))),
                new XElement("СодФХЖ4",
                    new XAttribute("НаимДокОпрПр", sellerDocumentInfo.DocumentName),
                    new XAttribute("Функция", GetFunctionCode(sellerDocumentInfo.Function)),
                    Attribute("ПорНомДокИнфПр", sellerDocumentInfo.DocumentNumber),
                    new XAttribute("ДатаДокИнфПр", sellerDocumentInfo.DocumentDate),
                    Attribute("ВидОпер", document.OperationType),
                    CreateAcceptanceInfo(document.AcceptanceInfo),
                    CreateOtherEconomicInfo("ИнфПолФХЖ4", document.OtherEconomicInfo)),
                CreateBuyerInfoCircumPublicProc(document.BuyerInfoCircumPublicProc),
                document.Signers.Select(CreateSigner),
                CreateDocumentDetails("ОснДоверОргСост", document.DocumentCreatorBase));

        #region СвПрин

        private static XElement CreateAcceptanceInfo(EdoAcceptanceInfo info)
            => new("СвПрин",
                Attribute("СодОпер", info.OperationName),
                DateAttribute("ДатаПрин", info.Date),
                new XElement("КодСодОпер",
                    new XAttribute("КодИтога", (int)info.OperationNameInfo.Code),
                    CreateDocumentDetails("РеквДокРасх", info.OperationNameInfo.DiscrepancyDocument)),
                CreateReceiverPerson(info.ReceiverPerson));

        private static XElement? CreateReceiverPerson(EdoReceiverPerson? receiverPerson)
        {
            if (receiverPerson == null)
                return null;

            if (receiverPerson.Employee is { } employee)
            {
                return new XElement("СвЛицПрин",
                    new XElement("РабОргПок",
                        new XAttribute("Должность", employee.JobTitle),
                        Attribute("ИныеСвед", employee.OtherInfo),
                        CreatePersonName(employee)));
            }

            if (receiverPerson.OtherIssuer?.OrganizationPerson is { } organizationPerson)
            {
                return new XElement("СвЛицПрин", new XElement("ИнЛицо",
                    new XElement("ПредОргПрин",
                        new XAttribute("Должность", organizationPerson.JobTitle),
                        Attribute("ИныеСвед", organizationPerson.OtherInfo),
                        new XAttribute("НаимОргПрин", organizationPerson.OrganizationName),
                        Attribute("ИННОргПрин", organizationPerson.OrganizationInn),
                        CreateDocumentDetails("ОснДоверОргПрин", organizationPerson.OrganizationBase),
                        CreateDocumentDetails("ОснПолнПредПрин", organizationPerson.EmployeeBase),
                        CreatePersonName(organizationPerson))));
            }

            if (receiverPerson.OtherIssuer?.PhysicalPerson is { } physicalPerson)
            {
                return new XElement("СвЛицПрин", new XElement("ИнЛицо",
                    new XElement("ФЛПрин",
                        Attribute("ИННФЛПрин", physicalPerson.PersonInn),
                        Attribute("ИныеСвед", physicalPerson.OtherInfo),
                        CreateDocumentDetails("ОснДоверФЛ", physicalPerson.PersonBase),
                        CreatePersonName(physicalPerson))));
            }

            throw new ArgumentException("Не указаны сведения о лице, принявшем товар (ReceiverPerson).");
        }

        #endregion

        #region ИнфПокЗаГоскКазн

        // Имя элемента - как в приказе 970 (таблица 7.14), не опечатка: ИнфПокЗаГоскКазн.
        private static XElement? CreateBuyerInfoCircumPublicProc(EdoBuyerInfoCircumPublicProc? info)
            => info == null
                ? null
                : new XElement("ИнфПокЗаГоскКазн",
                    Attribute("ИдКодЗак", info.PurchasingIdentificationCode),
                    new XAttribute("ЛицСчетПок", info.PersonalAccountBuyer),
                    new XAttribute("НаимФинОргПок", info.BuyerFinancialAuthorityName),
                    new XAttribute("НомРеестрЗапПок", info.BuyerBudgetRegisterNumber),
                    Attribute("УчНомБюдОбязПок", info.BuyerBudgetObligationAccountNumber),
                    Attribute("КодКазначПок", info.BuyerTreasuryCode),
                    Attribute("НаимКазначПок", info.BuyerTreasuryName),
                    new XAttribute("ОКТМОПок", info.BuyerMunicipalCode),
                    Attribute("ОКТМОМесПост", info.DeliveryMunicipalCode),
                    DateAttribute("ДатаОплПред", info.PaymentDate),
                    Attribute("УчНомДенОбяз", info.FinancialObligationAccountNumber),
                    Attribute("ОчерПлат", info.PaymentOrder),
                    info.PaymentType != null ? new XAttribute("ВидПлат", (int)info.PaymentType.Value) : null,
                    info.FinancialObligationInfoList.Select(x => new XElement("ИнфСведДенОбяз",
                        new XAttribute("НомСтр", x.RowNumber),
                        Attribute("КодОбъектФАИП", x.ObjectFAIPCode),
                        new XAttribute("ВидСредств", (int)x.FundType),
                        new XAttribute("КодПокБюджКласс", x.BuyerBudgetClassCode),
                        Attribute("КодЦелиПокуп", x.BuyerTargetCode),
                        new XAttribute("СумАванс", FormatAmount(x.AmountAdvance)))));

        #endregion
    }
}
