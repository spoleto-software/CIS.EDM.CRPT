using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Spoleto.TrueApi.Edo.Enums;
using Spoleto.TrueApi.Edo.Models.Upd;

namespace Spoleto.TrueApi.Edo.Helpers
{
    /// <summary>
    /// Формирование файлов информации продавца и покупателя УПД / УПДи формата 970 (версия 5.03).
    /// </summary>
    /// <remarks>
    /// Порядок элементов, форматы чисел и дат повторяют генератор CIS.EDM.CRPT (XmlHelperV5_03), по которому документы уже отправлялись.
    /// Информация покупателя - в EdoUniversalTransferDocumentXmlHelper.Buyer.cs.
    /// </remarks>
    internal static partial class EdoUniversalTransferDocumentXmlHelper
    {
        private const string FileIdPrefix = "ON_NSCHFDOPPR";
        private const string FormatVersion = "5.03";
        private const string TaxDocumentCode = "1115131";

        private const string DateFormat = "dd.MM.yyyy";
        private const string TimeFormat = "HH.mm.ss";
        private const string AmountFormat = "0.00";
        private const string QuantityFormat = "0.######";

        /// <summary>
        /// Кодировка файла: windows-1251, как требует формат.
        /// </summary>
        public static readonly Encoding FileEncoding;

        static EdoUniversalTransferDocumentXmlHelper()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//support for "windows-1251"
            FileEncoding = Encoding.GetEncoding("windows-1251");
        }

        /// <summary>
        /// Идентификатор файла: ON_NSCHFDOPPR_{получатель}_{отправитель}_{ГГГГММДД}_{GUID}_0_N3_N4_N5_N6_00.
        /// </summary>
        public static string GetFileId(EdoSellerUniversalTransferDocument document)
        {
            if (string.IsNullOrEmpty(document.SenderEdoId) || string.IsNullOrEmpty(document.RecipientEdoId))
            {
                throw new ArgumentException("Не указаны идентификаторы участников ЭДО отправителя и получателя (SenderEdoId, RecipientEdoId).", nameof(document));
            }

            var isMarked = document.Items.Any(x => x.AdditionalInfo?.ItemIdentificationNumberList?.Count > 0);
            var categories = document.GoodsCategories ?? (isMarked ? [EdoGoodsCategory.Marked] : []);
            string Flag(EdoGoodsCategory category) => categories.Contains(category) ? "1" : "0";

            // N2 (прослеживаемость) всегда 0: «ЭДО Лайт» не принимает прослеживаемые товары.
            return $"{FileIdPrefix}_{document.RecipientEdoId}_{document.SenderEdoId}_{document.DateCreation.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}_{Guid.NewGuid():D}"
                + $"_0_{Flag(EdoGoodsCategory.Marked)}_{Flag(EdoGoodsCategory.Alcohol)}_{Flag(EdoGoodsCategory.Tobacco)}_{Flag(EdoGoodsCategory.Oil)}_00";
        }

        /// <summary>
        /// XML файла информации продавца с объявлением кодировки windows-1251.
        /// </summary>
        public static string CreateXml(EdoSellerUniversalTransferDocument document, string fileId)
        {
            Validate(document);

            return CreateFile(fileId, document.ApplicationCreator, CreateDocument(document));
        }

        private static string CreateFile(string fileId, string applicationCreator, XElement document)
        {
            var xml = new XDocument(
                new XDeclaration("1.0", "windows-1251", null),
                new XElement("Файл",
                    new XAttribute("ИдФайл", fileId),
                    new XAttribute("ВерсФорм", FormatVersion),
                    new XAttribute("ВерсПрог", applicationCreator),
                    document));

            return xml.Declaration + Environment.NewLine + xml;
        }

        private static void Validate(EdoSellerUniversalTransferDocument document)
        {
            if (document.Sellers == null || document.Sellers.Count == 0)
                throw new ArgumentException("Не указан продавец (Sellers).", nameof(document));

            if (document.Buyers == null || document.Buyers.Count == 0)
                throw new ArgumentException("Не указан покупатель (Buyers).", nameof(document));

            if (document.Items == null || document.Items.Count == 0)
                throw new ArgumentException("Не указаны строки таблицы счета-фактуры (Items).", nameof(document));

            if (document.Signers == null || document.Signers.Count == 0)
                throw new ArgumentException("Не указан подписант (Signers).", nameof(document));

            if (document.IsUPDi && document.RevisionDate == null)
                throw new ArgumentException("Для исправления не указана дата исправления (RevisionDate).", nameof(document));
        }

        private static XElement CreateDocument(EdoSellerUniversalTransferDocument document)
            => new("Документ",
                new XAttribute("КНД", TaxDocumentCode),
                Attribute("УИД", document.DocumentUid),
                new XAttribute("Функция", GetFunctionCode(document.Function)),
                Attribute("ПоФактХЖ", string.IsNullOrEmpty(document.DocumentEconomicName) ? GetDocumentEconomicName(document.Function) : document.DocumentEconomicName),
                Attribute("НаимДокОпр", string.IsNullOrEmpty(document.DocumentName) ? GetDocumentName(document.Function) : document.DocumentName),
                DateAttribute("ДатаИнфПр", document.DateCreation),
                new XAttribute("ВремИнфПр", document.DateCreation.ToString(TimeFormat, CultureInfo.InvariantCulture)),
                new XAttribute("НаимЭконСубСост", GetEconomicEntityName(document.DocumentCreator)),
                Attribute("СоглСтрДопИнф", document.ApprovedStructureAdditionalInfoFields),
                CreateInvoiceInfo(document),
                CreateItemTable(document.Items),
                CreateTransferInfo(document.TransferInfo),
                document.Signers.Select(CreateSigner),
                CreateDocumentDetails("ОснДоверОргСост", document.DocumentCreatorBase));

        #region СвСчФакт

        private static XElement CreateInvoiceInfo(EdoSellerUniversalTransferDocument document)
            => new("СвСчФакт",
                new XAttribute("НомерДок", document.DocumentNumber),
                DateAttribute("ДатаДок", document.DocumentDate),
                Attribute("ИмяФайлИспрПрод", document.CorrectedSellerFileName),
                Attribute("ИмяФайлИспрПок", document.CorrectedBuyerFileName),
                document.IsUPDi
                    ? new XElement("ИспрДок", new XAttribute("НомИспр", document.RevisionNumber!), DateAttribute("ДатаИспр", document.RevisionDate!.Value))
                    : null,
                document.Sellers.Select(x => CreateOrganization("СвПрод", x)),
                // ГрузОт пишется без «он же»: печатная форма счета-фактуры его не поддерживает.
                document.Shippers?.Select(x => new XElement("ГрузОт", CreateOrganization("ГрузОтпр", x))),
                document.Consignees?.Select(x => CreateOrganization("ГрузПолуч", x)),
                document.PaymentDocumentInfoList?.Select(x => new XElement("СвПРД",
                    Attribute("НомерПРД", x.Number),
                    DateAttribute("ДатаПРД", x.Date),
                    AmountAttribute("СуммаПРД", x.Total))),
                CreateDocumentDetails("ДокПодтвОтгрНом", document.DocumentShipmentList),
                document.Buyers.Select(x => CreateOrganization("СвПокуп", x)),
                CreateCurrency(document),
                CreateAdditionalTransactionParticipantInfo(document),
                CreateOtherEconomicInfo("ИнфПолФХЖ1", document.OtherEconomicInfo));

        private static XElement? CreateCurrency(EdoSellerUniversalTransferDocument document)
        {
            if (string.IsNullOrEmpty(document.CurrencyCode))
                return null;

            return new XElement("ДенИзм",
                new XAttribute("КодОКВ", document.CurrencyCode),
                Attribute("НаимОКВ", document.CurrencyName),
                document.CurrencyRate > 0M ? new XAttribute("КурсВал", document.CurrencyRate.Value.ToString("0.00##", CultureInfo.InvariantCulture)) : null);
        }

        private static XElement? CreateAdditionalTransactionParticipantInfo(EdoSellerUniversalTransferDocument document)
        {
            var info = document.AdditionalTransactionParticipantInfo;
            if (info == null)
                return null;

            var element = new XElement("ДопСвФХЖ1",
                Attribute("ИдГосКон", info.GovernmentContractInfo),
                document.Function == EdoDocumentFunction.Invoice && info.InvoiceFormationType != EdoInvoiceFormationType.NotSpecified
                    ? new XAttribute("СпОбстФСЧФ", (int)info.InvoiceFormationType)
                    : null,
                document.Function == EdoDocumentFunction.InvoiceAndPrimaryDocument ? Attribute("СпОбстФСЧФДОП", info.UPDInvoiceFormationType) : null,
                document.Function == EdoDocumentFunction.PrimaryDocument ? Attribute("СпОбстФДОП", info.UPDFormationType) : null,
                info.ObligationInfoList?.Select(x => new XElement("ВидОбяз", Attribute("КодВидОбяз", x.Code), Attribute("НаимВидОбяз", x.Name))),
                CreateSellerInfoCircumPublicProc(info.SellerInfoCircumPublicProc),
                info.FactorInfo == null ? null : CreateOrganization("СвФактор", info.FactorInfo),
                CreateDocumentDetails("ОснУстДенТреб", info.MainAssignMonetaryClaim),
                CreateDocumentDetails("СопрДокФХЖ", info.SupportingDocumentList));

            return element.HasAttributes || element.HasElements ? element : null;
        }

        private static XElement? CreateSellerInfoCircumPublicProc(EdoSellerInfoCircumPublicProc? info)
            => info == null
                ? null
                : new XElement("ИнфПродЗаГосКазн",
                    DateAttribute("ДатаГосКонт", info.DateStateContract),
                    new XAttribute("НомерГосКонт", info.NumberStateContract),
                    Attribute("ЛицСчетПрод", info.PersonalAccountSeller),
                    Attribute("КодПродБюджКласс", info.SellerBudgetClassCode),
                    Attribute("КодЦелиПрод", info.SellerTargetCode),
                    Attribute("КодКазначПрод", info.SellerTreasuryCode),
                    Attribute("НаимКазначПрод", info.SellerTreasuryName));

        #endregion

        #region Участники

        private static XElement CreateOrganization(string elementName, EdoOrganization organization)
            => new(elementName,
                Attribute("ОКПО", organization.Okpo),
                Attribute("КодОПФ", organization.OpfCode),
                Attribute("ПолнНаимОПФ", organization.OpfName),
                Attribute("СтруктПодр", organization.Department),
                Attribute("ИнфДляУчаст", organization.OrganizationAdditionalInfo),
                Attribute("СокрНаим", organization.ShortName),
                new XElement("ИдСв", CreateIdentification(organization)),
                CreateAddress(organization.Address),
                CreateBankAccountDetails(organization.BankAccountDetails),
                CreateContact(organization.Contact));

        private static XElement CreateIdentification(EdoOrganization organization)
        {
            var info = organization.OrganizationIdentificationInfo;

            if (info.IndividualEntrepreneur is { } individualEntrepreneur)
            {
                return new XElement("СвИП",
                    new XAttribute("ИННФЛ", individualEntrepreneur.Inn),
                    Attribute("СвГосРегИП", individualEntrepreneur.IndividualEntrepreneurRegistrationCertificate),
                    Attribute("ОГРНИП", individualEntrepreneur.Ogrnip),
                    DateAttribute("ДатаОГРНИП", individualEntrepreneur.OgrnipDate),
                    Attribute("ИныеСвед", individualEntrepreneur.OtherInfo),
                    CreatePersonName(individualEntrepreneur));
            }

            if (info.LegalPerson is { } legalPerson)
            {
                return new XElement("СвЮЛУч",
                    new XAttribute("НаимОрг", legalPerson.Name),
                    new XAttribute("ИННЮЛ", legalPerson.Inn),
                    Attribute("КПП", legalPerson.Kpp));
            }

            if (info.PhysicalPerson is { } physicalPerson)
            {
                return new XElement("СвФЛУч",
                    Attribute("ИННФЛ", physicalPerson.Inn),
                    Attribute("ИдСтатЛ", physicalPerson.Status),
                    Attribute("ИныеСвед", physicalPerson.OtherInfo),
                    CreatePersonName(physicalPerson));
            }

            if (info.ForeignPerson is { } foreignPerson)
            {
                return CreateForeignEntity("СвИнНеУч", foreignPerson);
            }

            throw new ArgumentException($"Не указаны идентификационные сведения участника «{organization.ShortName}» (OrganizationIdentificationInfo).");
        }

        private static string GetEconomicEntityName(EdoOrganization organization)
        {
            var info = organization.OrganizationIdentificationInfo;

            if (info.LegalPerson is { } legalPerson)
            {
                if (!string.IsNullOrEmpty(legalPerson.Inn) && !string.IsNullOrEmpty(legalPerson.Kpp))
                    return $"{legalPerson.Name}, ИНН/КПП: {legalPerson.Inn}/{legalPerson.Kpp}";

                return string.IsNullOrEmpty(legalPerson.Inn) ? legalPerson.Name : $"{legalPerson.Name}, ИНН: {legalPerson.Inn}";
            }

            if (info.IndividualEntrepreneur is { } individualEntrepreneur)
            {
                var name = $"ИП {GetFullName(individualEntrepreneur)}";
                return string.IsNullOrEmpty(individualEntrepreneur.Inn) ? name : $"{name}, ИНН: {individualEntrepreneur.Inn}";
            }

            if (info.PhysicalPerson is { } physicalPerson)
            {
                var name = GetFullName(physicalPerson);
                return string.IsNullOrEmpty(physicalPerson.Inn) ? name : $"{name}, ИНН: {physicalPerson.Inn}";
            }

            if (info.ForeignPerson is { } foreignPerson)
                return foreignPerson.Name;

            throw new ArgumentException("Не указаны идентификационные сведения составителя файла (DocumentCreator).");
        }

        private static XElement CreateForeignEntity(string elementName, EdoForeignEntity foreignEntity)
            => new(elementName,
                new XAttribute("ИдСтат", foreignEntity.Status),
                new XAttribute("КодСтр", foreignEntity.CountryCode),
                new XAttribute("НаимСтран", foreignEntity.CountryName),
                new XAttribute("Наим", foreignEntity.Name),
                Attribute("Идентиф", foreignEntity.Identifier),
                Attribute("ИныеСвед", foreignEntity.OtherInfo));

        private static XElement? CreateAddress(EdoAddress? address)
        {
            if (address == null)
                return null;

            object? content = null;
            if (address.RussianAddress is { } russianAddress)
            {
                content = new XElement("АдрРФ",
                    Attribute("Индекс", russianAddress.ZipCode),
                    new XAttribute("КодРегион", russianAddress.RegionCode),
                    new XAttribute("НаимРегион", russianAddress.Region),
                    Attribute("Район", russianAddress.Territory),
                    Attribute("Город", russianAddress.City),
                    Attribute("НаселПункт", russianAddress.Locality),
                    Attribute("Улица", russianAddress.Street),
                    Attribute("Дом", russianAddress.Building),
                    Attribute("Корпус", russianAddress.Block),
                    Attribute("Кварт", russianAddress.Apartment),
                    Attribute("ИныеСвед", russianAddress.OtherInfo));
            }
            else if (address.AddressCode is { } addressCode)
            {
                content = new XElement("АдрГАР",
                    new XAttribute("ИдНом", addressCode.UniqueCode.ToString("D")),
                    Attribute("Индекс", addressCode.ZipCode),
                    TextElement("Регион", addressCode.Region),
                    TextElement("НаимРегион", addressCode.RegionName),
                    CreateAddressElement("МуниципРайон", addressCode.MunicipalDistrict),
                    CreateAddressElement("ГородСелПоселен", addressCode.UrbanRuralSettlement),
                    CreateAddressElement("НаселенПункт", "Вид", addressCode.Settlement),
                    CreateAddressElement("ЭлПланСтруктур", "Тип", addressCode.PlanningStructureElement),
                    CreateAddressElement("ЭлУлДорСети", "Тип", addressCode.StreetRoadNetworkElement),
                    TextElement("ЗемелУчасток", addressCode.LandPlotNumber),
                    addressCode.BuildingList?.Select(x => CreateAddressElement("Здание", x)),
                    CreateAddressElement("ПомещЗдания", addressCode.BuildingRoom),
                    CreateAddressElement("ПомещКвартиры", addressCode.ApartmentRoom));
            }
            else if (address.AddressInformation is { } addressInformation)
            {
                content = new XElement("АдрИнф",
                    new XAttribute("КодСтр", addressInformation.CountryCode),
                    new XAttribute("НаимСтран", addressInformation.CountryName),
                    new XAttribute("АдрТекст", addressInformation.Address));
            }

            return content == null ? null : new XElement("Адрес", Attribute("ГЛНМеста", address.GlobalLocationNumber), content);
        }

        private static XElement? CreateAddressElement(string elementName, EdoAddressElementCode? element)
            => element == null ? null : new XElement(elementName, new XAttribute("ВидКод", element.TypeCode), new XAttribute("Наим", element.Name));

        private static XElement? CreateAddressElement(string elementName, string typeAttributeName, EdoAddressElement? element)
            => element == null ? null : new XElement(elementName, new XAttribute(typeAttributeName, element.Type), new XAttribute("Наим", element.Name));

        private static XElement? CreateAddressElement(string elementName, EdoAddressElementNumber? element)
            => element == null ? null : new XElement(elementName, new XAttribute("Тип", element.Type), new XAttribute("Номер", element.Number));

        private static XElement? CreateBankAccountDetails(EdoBankAccountDetails? bankAccountDetails)
            => bankAccountDetails == null
                ? null
                : new XElement("БанкРекв",
                    Attribute("НомерСчета", bankAccountDetails.BankAccountNumber),
                    bankAccountDetails.BankDetails is { } bankDetails
                        ? new XElement("СвБанк",
                            Attribute("НаимБанк", bankDetails.BankName),
                            Attribute("БИК", bankDetails.BankId),
                            Attribute("КорСчет", bankDetails.CorrespondentAccount))
                        : null);

        private static XElement? CreateContact(EdoContact? contact)
            => contact == null
                ? null
                : new XElement("Контакт",
                    Attribute("ИнКонт", contact.OtherInfo),
                    contact.PhoneList?.Select(x => TextElement("Тлф", x)),
                    contact.EmailList?.Select(x => TextElement("ЭлПочта", x)));

        private static XElement CreatePersonName(EdoPerson person)
            => new("ФИО",
                new XAttribute("Фамилия", person.Surname),
                new XAttribute("Имя", person.FirstName),
                Attribute("Отчество", person.Patronymic));

        private static string GetFullName(EdoPerson person)
            => string.Join(" ", new[] { person.Surname, person.FirstName, person.Patronymic }.Where(x => !string.IsNullOrEmpty(x)));

        #endregion

        #region ТаблСчФакт

        private static XElement CreateItemTable(List<EdoInvoiceItem> items)
            => new("ТаблСчФакт",
                items.Select((x, i) => CreateItem(x, i + 1)),
                CreateItemTotal(items));

        private static XElement CreateItem(EdoInvoiceItem item, int lineNumber)
            => new("СведТов",
                new XAttribute("НомСтр", lineNumber),
                new XAttribute("НаимТов", item.ProductName),
                Attribute("ОКЕИ_Тов", item.UnitCode),
                Attribute("НаимЕдИзм", item.UnitName),
                item.Quantity > 0 ? new XAttribute("КолТов", FormatQuantity(item.Quantity)) : null,
                item.Price > 0 ? new XAttribute("ЦенаТов", FormatAmount(item.Price)) : null,
                item.SumWithoutVat > 0 ? new XAttribute("СтТовБезНДС", FormatAmount(item.SumWithoutVat)) : null,
                new XAttribute("НалСт", GetTaxRateName(item.TaxRate)),
                new XAttribute("СтТовУчНал", FormatAmount(item.Sum)),
                item.CustomsDeclarationList?.Select(x => new XElement("СвДТ",
                    Attribute("КодПроисх", x.CountryCode),
                    Attribute("НомерДТ", x.DeclarationNumber))),
                CreateItemAdditionalInfo(item.AdditionalInfo),
                new XElement("Акциз", item.Excise == null
                    ? new XElement("БезАкциз", "без акциза")
                    : new XElement("СумАкциз", FormatAmount(item.Excise.Value))),
                CreateVat("СумНал", new EdoVatAmount { Amount = item.Vat, WithoutVat = item.TaxRate == EdoTaxRate.WithoutVat }, $"строки {lineNumber}"),
                item.OtherEconomicInfoItemList?.Select(x => new XElement("ИнфПолФХЖ2", Attribute("Идентиф", x.Id), Attribute("Значен", x.Value))));

        private static XElement CreateItemTotal(List<EdoInvoiceItem> items)
        {
            var sumWithoutVat = items.Sum(x => x.SumWithoutVat);
            var quantity = items.Sum(x => x.Quantity);
            var vat = new EdoVatAmount { Amount = items.Sum(x => x.Vat), WithoutVat = items.All(x => x.TaxRate == EdoTaxRate.WithoutVat) };

            return new XElement("ВсегоОпл",
                sumWithoutVat > 0 ? new XAttribute("СтТовБезНДСВсего", FormatAmount(sumWithoutVat)) : null,
                new XAttribute("СтТовУчНалВсего", FormatAmount(items.Sum(x => x.Sum))),
                quantity > 0 ? new XAttribute("КолНеттоВс", FormatQuantity(quantity)) : null,
                CreateVat("СумНалВсего", vat, "строки «Всего к оплате»"));
        }

        private static XElement? CreateItemAdditionalInfo(EdoInvoiceItemAdditionalInfo? info)
            => info == null
                ? null
                : new XElement("ДопСведТов",
                    info.Type != EdoInvoiceItemType.NotSpecified ? new XAttribute("ПрТовРаб", (int)info.Type) : null,
                    Attribute("ДопПризн", info.AdditionalTypeInfo),
                    info.ItemToRelease != null ? new XAttribute("НадлОтп", info.ItemToRelease.Value.ToString("0.#######", CultureInfo.InvariantCulture)) : null,
                    Attribute("ХарактерТов", info.Characteristic),
                    Attribute("СортТов", info.Kind),
                    Attribute("СерияТов", info.Series),
                    Attribute("АртикулТов", info.Article),
                    Attribute("КодТов", info.Code),
                    Attribute("ГТИН", info.GlobalTradeItemNumber),
                    Attribute("КодКат", info.CatalogCode),
                    Attribute("КодВидТов", info.FeaccCode),
                    Attribute("КодВидПр", info.ProductTypeCode),
                    Attribute("КодТовОКДП2", info.Okdp2ProductCode),
                    Attribute("ДопИнфПВидО", info.AdditionalOperationInfo),
                    info.CountryNames?.Select(x => new XElement("КрНаимСтрПр", x)),
                    CreateDocumentDetails("СопрДокТов", info.SupportingDocumentList),
                    info.TaxAccountingAmortization is { } amortization
                        ? new XElement("НалУчАморт",
                            new XAttribute("АмГруппа", amortization.AmortizationGroup),
                            new XAttribute("КодОКОФ", amortization.OkofCode),
                            new XAttribute("СрПолИспОС", amortization.EstablishedUsefulLife),
                            new XAttribute("ФактСрокИсп", amortization.ActualUsefulLifeMonths))
                        : null,
                    info.RecoveredTaxAmount == null ? null : CreateVat("СумНалВосст", info.RecoveredTaxAmount, "восстановленного налога"),
                    info.ItemIdentificationNumberList?.Select(CreateItemIdentificationNumber),
                    info.GovernmentSystemAdditionalInfoList?.Select(x => new XElement("СвГосСист",
                        new XAttribute("НаимГосСист", x.InformationSystemName),
                        Attribute("УчетЕд", x.AccountingUnit),
                        Attribute("ИнаяИнф", x.OtherInfo),
                        x.AccountingUnitIds.Select(id => new XElement("ИдНомУчетЕд", id)))));

        private static XElement CreateItemIdentificationNumber(EdoInvoiceItemIdentificationNumber number)
            => new("НомСредИдентТов",
                Attribute("ИдентТрансУпак", number.PackageId),
                Attribute("КолВедМарк", number.MarkedProductQuantity),
                Attribute("ПрПартМарк", number.ProductionBatchCode),
                number.MarkItems != null
                    ? number.MarkItems.Select(x => new XElement("КИЗ", x))
                    : number.SecondaryPackageItems?.Select(x => new XElement("НомУпак", x)));

        private static XElement CreateVat(string elementName, EdoVatAmount vat, string target)
        {
            if (vat.WithoutVat)
                return new XElement(elementName, new XElement("БезНДС", "без НДС"));

            if (vat.Amount == null)
                throw new ArgumentException($"Не указана сумма НДС для {target}.");

            return new XElement(elementName, new XElement("СумНал", FormatAmount(vat.Amount.Value)));
        }

        #endregion

        #region СвПродПер

        private static XElement? CreateTransferInfo(EdoTransferInfo? transferInfo)
            => transferInfo == null
                ? null
                : new XElement("СвПродПер",
                    CreateTransferDetails(transferInfo.TransferDetails),
                    CreateOtherEconomicInfo("ИнфПолФХЖ3", transferInfo.OtherEconomicInfo));

        private static XElement CreateTransferDetails(EdoTransferDetails details)
            => new("СвПер",
                new XAttribute("СодОпер", details.OperationName),
                Attribute("ВидОпер", details.OperationType),
                DateAttribute("ДатаПер", details.Date),
                DateAttribute("ДатаНачПер", details.StartDate),
                DateAttribute("ДатаОконПер", details.EndDate),
                details.TransferDocuments?.Count > 0
                    ? CreateDocumentDetails("ОснПер", details.TransferDocuments)
                    : new XElement("БезДокОснПер", "1"),
                CreateSenderPerson(details.SenderPerson),
                details.Transportation is { } transportation
                    ? new XElement("Тран",
                        Attribute("СвТран", transportation.TransferTextInfo),
                        Attribute("Инкотермс", transportation.Incoterms),
                        Attribute("ВерИнкотермс", transportation.IncotermsVersion))
                    : null,
                details.CreatedThingInfo is { } createdThingInfo
                    ? new XElement("СвПерВещи",
                        DateAttribute("ДатаПерВещ", createdThingInfo.Date),
                        Attribute("СвПерВещ", createdThingInfo.Information),
                        CreateDocumentDetails("ДокПерВещ", createdThingInfo.Document))
                    : null);

        private static XElement? CreateSenderPerson(EdoSenderPerson? senderPerson)
        {
            if (senderPerson == null)
                return null;

            if (senderPerson.Employee is { } employee)
            {
                return new XElement("СвЛицПер",
                    new XElement("РабОргПрод",
                        new XAttribute("Должность", employee.JobTitle),
                        Attribute("ИныеСвед", employee.OtherInfo),
                        CreatePersonName(employee)));
            }

            if (senderPerson.OtherIssuer?.OrganizationPerson is { } organizationPerson)
            {
                return new XElement("СвЛицПер", new XElement("ИнЛицо",
                    new XElement("ПредОргПер",
                        new XAttribute("Должность", organizationPerson.JobTitle),
                        Attribute("ИныеСвед", organizationPerson.OtherInfo),
                        new XAttribute("НаимОргПер", organizationPerson.OrganizationName),
                        Attribute("ИННЮЛПер", organizationPerson.OrganizationInn),
                        CreateDocumentDetails("ОснДоверОргПер", organizationPerson.OrganizationBase),
                        CreateDocumentDetails("ОснПолнПредПер", organizationPerson.EmployeeBase),
                        CreatePersonName(organizationPerson))));
            }

            if (senderPerson.OtherIssuer?.PhysicalPerson is { } physicalPerson)
            {
                return new XElement("СвЛицПер", new XElement("ИнЛицо",
                    new XElement("ФЛПер",
                        Attribute("ИННФЛПер", physicalPerson.PersonInn),
                        Attribute("ИныеСвед", physicalPerson.OtherInfo),
                        CreateDocumentDetails("ОснДоверФЛ", physicalPerson.PersonBase),
                        CreatePersonName(physicalPerson))));
            }

            throw new ArgumentException("Не указаны сведения о лице, передавшем товар (SenderPerson).");
        }

        #endregion

        #region Подписант

        private static XElement CreateSigner(EdoSigner signer)
            => new("Подписант",
                Attribute("Должн", signer.JobTitle),
                signer.SignatureType != null ? new XAttribute("ТипПодпис", (int)signer.SignatureType.Value) : null,
                DateAttribute("ДатаПодДок", signer.SigningDate),
                new XAttribute("СпосПодтПолном", (int)signer.AuthorityConfirmationType),
                Attribute("ДопСведПодп", signer.AdditionalInfo),
                CreatePersonName(signer.Person),
                signer.ElectronicPoAInfo is { } electronicPoA
                    ? new XElement("СвДоверЭл",
                        new XAttribute("НомДовер", electronicPoA.RegistrationNumber.ToString("D")),
                        DateAttribute("ДатаВыдДовер", electronicPoA.IssueDate),
                        Attribute("ВнНомДовер", electronicPoA.InternalNumber),
                        DateAttribute("ДатаВнРегДовер", electronicPoA.InternalRegistrationDate),
                        new XAttribute("ИдСистХран", electronicPoA.SystemIdentification),
                        Attribute("УРЛСист", electronicPoA.SystemUrl))
                    : signer.PaperPoAInfo is { } paperPoA
                        ? new XElement("СвДоверБум",
                            DateAttribute("ДатаВыдДовер", paperPoA.IssueDate),
                            new XAttribute("ВнНомДовер", paperPoA.InternalNumber),
                            Attribute("СвИдДовер", paperPoA.TrusteeInfo),
                            paperPoA.Signer == null ? null : CreatePersonName(paperPoA.Signer))
                        : null);

        #endregion

        #region Общие типы

        /// <summary>
        /// Реквизиты документа (РеквДокТип).
        /// </summary>
        private static IEnumerable<XElement>? CreateDocumentDetails(string elementName, List<EdoDocumentDetails>? documents)
            => documents?.Select(x => CreateDocumentDetails(elementName, x)!);

        private static XElement? CreateDocumentDetails(string elementName, EdoDocumentDetails? document)
            => document == null
                ? null
                : new XElement(elementName,
                    new XAttribute("РеквНаимДок", document.Name),
                    new XAttribute("РеквНомерДок", document.Number),
                    DateAttribute("РеквДатаДок", document.Date),
                    Attribute("РеквИдФайлДок", document.FileId),
                    Attribute("РеквИдДок", document.DocumentId),
                    Attribute("РИдСистХранД", document.StorageSystemId),
                    Attribute("РеквУРЛСистДок", document.SystemUrl),
                    Attribute("РеквДопСведДок", document.OtherInfo),
                    document.Creators?.Select(CreateDocumentCreator));

        private static XElement CreateDocumentCreator(EdoDocumentCreator creator)
            => new("РеквИдРекСост",
                !string.IsNullOrEmpty(creator.InnLegalEntity) ? new XElement("ИННЮЛ", creator.InnLegalEntity)
                : !string.IsNullOrEmpty(creator.InnIndividual) ? new XElement("ИННФЛ", creator.InnIndividual)
                : creator.ForeignEntity != null ? CreateForeignEntity("ДаннИно", creator.ForeignEntity)
                : TextElement("НаимОИВ", creator.AuthorityShortName));

        /// <summary>
        /// Информационное поле факта хозяйственной жизни 1, 3 или 4.
        /// </summary>
        private static XElement? CreateOtherEconomicInfo(string elementName, EdoOtherEconomicInfo? info)
            => info == null
                ? null
                : new XElement(elementName,
                    Attribute("ИдФайлИнфПол", info.InfoFileId),
                    info.Items?.Select(x => new XElement("ТекстИнф", Attribute("Идентиф", x.Id), Attribute("Значен", x.Value))));

        private static XAttribute? Attribute(string name, string? value)
            => string.IsNullOrEmpty(value) ? null : new XAttribute(name, value);

        private static XAttribute? DateAttribute(string name, DateTime? value)
            => value == null ? null : new XAttribute(name, value.Value.ToString(DateFormat, CultureInfo.InvariantCulture));

        private static XAttribute? AmountAttribute(string name, decimal? value)
            => value == null ? null : new XAttribute(name, FormatAmount(value.Value));

        private static XElement? TextElement(string name, string? value)
            => string.IsNullOrEmpty(value) ? null : new XElement(name, value);

        private static string FormatAmount(decimal value) => value.ToString(AmountFormat, CultureInfo.InvariantCulture);

        private static string FormatQuantity(decimal value) => value.ToString(QuantityFormat, CultureInfo.InvariantCulture);

        private static string GetFunctionCode(EdoDocumentFunction function) => function switch
        {
            EdoDocumentFunction.Invoice => "СЧФ",
            EdoDocumentFunction.InvoiceAndPrimaryDocument => "СЧФДОП",
            EdoDocumentFunction.PrimaryDocument => "ДОП",
            EdoDocumentFunction.CommissionSale => "СвРК",
            EdoDocumentFunction.CommissionPurchase => "СвЗК",
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
        };

        private static string? GetDocumentName(EdoDocumentFunction function) => function switch
        {
            EdoDocumentFunction.InvoiceAndPrimaryDocument => "Счет-фактура и документ об отгрузке товаров (выполнении работ), передаче имущественных прав (документ об оказании услуг)",
            EdoDocumentFunction.PrimaryDocument => "Документ об отгрузке товаров (выполнении работ), передаче имущественных прав (документ об оказании услуг)",
            _ => null
        };

        private static string? GetDocumentEconomicName(EdoDocumentFunction function) => function switch
        {
            EdoDocumentFunction.InvoiceAndPrimaryDocument or EdoDocumentFunction.PrimaryDocument => "Документ об отгрузке товаров (выполнении работ), передаче имущественных прав (документ об оказании услуг)",
            EdoDocumentFunction.CommissionSale => "Документ, включающий в себя сведения о факте реализации комиссионером (агентом, действующим от собственного имени) товаров комитента (принципала)",
            EdoDocumentFunction.CommissionPurchase => "Документ, включающий в себя сведения о факте закупки комиссионером (агентом, действующим от собственного имени) товаров для комитента (принципала)",
            _ => null
        };

        private static string GetTaxRateName(EdoTaxRate taxRate) => taxRate switch
        {
            EdoTaxRate.WithoutVat => "без НДС",
            EdoTaxRate.Zero => "0%",
            EdoTaxRate.FivePercent => "5%",
            EdoTaxRate.SevenPercent => "7%",
            EdoTaxRate.TenPercent => "10%",
            EdoTaxRate.EighteenPercent => "18%",
            EdoTaxRate.TwentyPercent => "20%",
            EdoTaxRate.TenFraction => "10/110",
            EdoTaxRate.EighteenFraction => "18/118",
            EdoTaxRate.TwentyFraction => "20/120",
            EdoTaxRate.TaxedByAgent => "НДС исчисляется налоговым агентом",
            EdoTaxRate.TwentyTwoFraction => "22/122",
            EdoTaxRate.TwentyTwoPercent => "22%",
            _ => throw new ArgumentOutOfRangeException(nameof(taxRate), taxRate, null)
        };

        #endregion
    }
}
