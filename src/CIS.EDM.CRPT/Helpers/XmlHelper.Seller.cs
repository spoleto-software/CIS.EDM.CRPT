using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using CIS.EDM.CRPT.Extensions;
using CIS.EDM.Extensions;
using CIS.EDM.Models;
using CIS.EDM.Models.Reference;
using CIS.EDM.Models.V5_01.Seller;
using CIS.EDM.Models.V5_01.Seller.Address;
using CIS.EDM.Models.V5_01.Seller.Reference;

namespace CIS.EDM.CRPT.Helpers
{
    internal static partial class XmlHelper
    {
        private const string FileEncoding = "windows-1251";

        private const string quantityToStringPattern = "0.######";

        public readonly static Encoding DefaultEncoding;

        static XmlHelper()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//support for "windows-1251"
            DefaultEncoding = Encoding.GetEncoding(FileEncoding);
        }

        public static DocumentData GenerateXml(SellerUniversalTransferDocument sellerDataContract)
        {
            if (sellerDataContract.Buyers == null || sellerDataContract.Buyers.Count == 0)
                throw new ArgumentNullException("Не указан продавец.");

            if (sellerDataContract.Sellers == null || sellerDataContract.Sellers.Count == 0)
                throw new ArgumentNullException("Не указан покупатель.");

            if (sellerDataContract.SenderEdmParticipant == null)
                throw new ArgumentNullException("Не указан идентификатор отправителя УПД.");

            if (sellerDataContract.RecipientEdmParticipant == null)
                throw new ArgumentNullException("Не указан идентификатор получателя УПД.");

            if (sellerDataContract.DocumentCreator == null)
                throw new ArgumentNullException("Не указан экономический субъект - составитель файла обмена счета-фактуры");

            var xmlDocument = CreateXmlDocument(sellerDataContract);
            GenerateXmlFrom(xmlDocument, sellerDataContract);

            var xml = MakeXmlFormatted(xmlDocument);

            return new DocumentData
            {
                Id = sellerDataContract.FileId,
                Content = xml,
                FileEncoding = DefaultEncoding
            };
        }

        private static XmlDocument CreateXmlDocument(UniversalTransferDocumentBase dataContract)
        {
            var xmlDocument = new XmlDocument();
            var xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", FileEncoding, "yes");
            xmlDocument.AppendChild(xmlDeclaration);

            var fileNode = xmlDocument.CreateElement("Файл");
            fileNode.SetAttribute("ИдФайл", dataContract.FileId);
            fileNode.SetAttribute("ВерсФорм", dataContract.FormatVersion);
            fileNode.SetAttribute("ВерсПрог", dataContract.ApplicationCreator);
            xmlDocument.AppendChild(fileNode);

            return xmlDocument;
        }

        private static void GenerateXmlFrom(XmlDocument xmlDocument, SellerUniversalTransferDocument dataContract)
        {
            var headerNode = xmlDocument.CreateElement("СвУчДокОбор");
            headerNode.SetAttribute("ИдОтпр", dataContract.SenderEdmParticipant.FullId);
            headerNode.SetAttribute("ИдПол", dataContract.RecipientEdmParticipant.FullId);
            xmlDocument.DocumentElement.AppendChild(headerNode);

            var subHeaderNode = xmlDocument.CreateElement("СвОЭДОтпр");
            subHeaderNode.SetAttribute("НаимОрг", dataContract.EdmOperator.Name);
            subHeaderNode.SetAttribute("ИННЮЛ", dataContract.EdmOperator.Inn);
            subHeaderNode.SetAttribute("ИдЭДО", dataContract.EdmOperator.OperatorId);
            headerNode.AppendChild(subHeaderNode);

            var documentNode = xmlDocument.CreateElement("Документ");
            documentNode.SetAttribute("КНД", "1115131"); // Классификатор налоговых документов
            documentNode.SetAttribute("Функция", dataContract.Function.ToString());

            var documentEconomicName = !String.IsNullOrEmpty(dataContract.DocumentEconomicName) ? dataContract.DocumentEconomicName : dataContract.Function.GetDocumentEconomicName();
            if (documentEconomicName != null)
                documentNode.SetAttribute("ПоФактХЖ", documentEconomicName); // Наименование документа по факту хозяйственной жизни

            var documentName = !String.IsNullOrEmpty(dataContract.DocumentName) ? dataContract.DocumentName : dataContract.Function.GetDocumentName();
            if (documentName != null)
                documentNode.SetAttribute("НаимДокОпр", documentName); // Наименование первичного документа, определенное организацией (согласованное сторонами сделки)

            documentNode.SetAttribute("ДатаИнфПр", dataContract.DateCreation.ToString("dd.MM.yyyy")); // Дата формирования файла обмена счета-фактуры (информации продавца)
            documentNode.SetAttribute("ВремИнфПр", dataContract.DateCreation.ToString("HH.mm.ss")); // Время формирования файла обмена счета-фактуры (информации продавца)
            documentNode.SetAttribute("НаимЭконСубСост", GetEconomicEntityName(dataContract.DocumentCreator)); // Наименование экономического субъекта – составителя файла обмена счета-фактуры (информации продавца)

            if (!String.IsNullOrEmpty(dataContract.DocumentCreatorBase))
                documentNode.SetAttribute("ОснДоверОргСост", dataContract.DocumentCreatorBase); // Основание, по которому экономический субъект является составителем файла обмена счета-фактуры (информации продавца)

            if (!String.IsNullOrEmpty(dataContract.ApprovedStructureAdditionalInfoFields))
                documentNode.SetAttribute("СоглСтрДопИнф", dataContract.ApprovedStructureAdditionalInfoFields); // Информация о наличии согласованной структуры дополнительных информационных полей

            xmlDocument.DocumentElement.AppendChild(documentNode);

            // СвСчФакт
            AddDocumentInfo(xmlDocument, dataContract, documentNode);

            // ТаблСчФакт
            AddObjectiveItems(xmlDocument, documentNode, dataContract);

            // СвПродПер
            AddTransferInfo(xmlDocument, documentNode, dataContract);

            // Подписант
            AddSellerSigners(xmlDocument, documentNode, dataContract);
        }

        /// <summary>
        /// Сведения о счете-фактуре (содержание факта хозяйственной жизни 1 - сведения об участниках факта хозяйственной жизни, основаниях и обстоятельствах его проведения)
        /// </summary>
        /// <remarks>
        /// СвСчФакт
        /// </remarks>
        private static void AddDocumentInfo(XmlDocument xmlDocument, SellerUniversalTransferDocument dataContract, XmlElement documentNode)
        {
            var documentInfoNode = xmlDocument.CreateElement("СвСчФакт");
            documentInfoNode.SetAttribute("НомерСчФ", dataContract.DocumentNumber);
            documentInfoNode.SetAttribute("ДатаСчФ", dataContract.DocumentDate.ToString("dd.MM.yyyy"));
            documentInfoNode.SetAttribute("КодОКВ", dataContract.CurrencyCode); //Валюта: Код
            documentNode.AppendChild(documentInfoNode);

            AddRevisionInfo(xmlDocument, documentInfoNode, dataContract);

            // Сведения о продавце (строки 2, 2а, 2б счета-фактуры)
            foreach (var seller in dataContract.Sellers)
                AddFirmInformation(xmlDocument, documentInfoNode, seller, "СвПрод");

            if (dataContract.Shippers?.Count > 0)
            {
                foreach (var shipper in dataContract.Shippers)
                {
                    //if (IsOrganizationsEquals(dataContract.Seller, dataContract.Shipper))
                    //    AddSameFirmInformation(xmlDocument, documentInfoNode, "ГрузОт");
                    //else
                    //2022-02-02 NadymovOleg: Как оказалось печатная форма счёт-фактуры не поддерживает фичу "ОнЖе"
                    var shipperNode = xmlDocument.CreateElement("ГрузОт");
                    documentInfoNode.AppendChild(shipperNode);

                    // Сведения о грузоотправителе (строка 3 счета-фактуры)
                    AddFirmInformation(xmlDocument, shipperNode, shipper, "ГрузОтпр");
                }
            }

            if (dataContract.Consignees?.Count > 0)
            {
                foreach (var consignee in dataContract.Consignees)
                {
                    // в графе "ГрузПолуч" не работает фича "ОнЖе"
                    // Грузополучатель и его адрес (строка 4 счета-фактуры)
                    AddFirmInformation(xmlDocument, documentInfoNode, consignee, "ГрузПолуч");
                }
            }

            AddPaymentDocumentInfo(xmlDocument, documentInfoNode, dataContract.PaymentDocumentInfoList);

            // Сведения о покупателе (строки 6, 6а, 6б счета-фактуры)
            foreach (var buyer in dataContract.Buyers)
                AddFirmInformation(xmlDocument, documentInfoNode, buyer, "СвПокуп");

            // ДопСвФХЖ1
            AddAdditionalInfo(xmlDocument, documentInfoNode, dataContract);

            // ДокПодтвОтгр
            AddTransferDocumentShipmentInfo(xmlDocument, documentInfoNode, dataContract);

            // Информационное поле факта хозяйственной жизни 1
            AddOtherEconomicInfo(xmlDocument, documentInfoNode, dataContract.OtherEconomicInfo, "ИнфПолФХЖ1");
        }

        /// <summary>
        /// Исправление (строка 1а счета - фактуры)
        /// </summary>
        private static void AddRevisionInfo(XmlDocument xmlDocument, XmlElement parentElement, SellerUniversalTransferDocument dataContract)
        {
            var revisionNode = xmlDocument.CreateElement("ИспрСчФ"); // Исправление (строка 1а счета - фактуры)

            //Для отправки исходного УПД без номера и даты исправления всё равно нужно формировать тег «ИспрСчФ» с атрибутами «ДефНомИспрСчФ="-"» и «ДефДатаИспрСчФ="-"»
            if (dataContract.IsHyphenRevisionNumber)
                revisionNode.SetAttribute("ДефНомИспрСчФ", "-");
            else if (!String.IsNullOrEmpty(dataContract.RevisionNumber))
                revisionNode.SetAttribute("НомИспрСчФ", dataContract.RevisionNumber);

            if (dataContract.IsHyphenRevisionDate)
                revisionNode.SetAttribute("ДефДатаИспрСчФ", "-");
            else if (dataContract.RevisionDate != null)
                revisionNode.SetAttribute("ДатаИспрСчФ", dataContract.RevisionDate.Value.ToString("dd.MM.yyyy"));

            parentElement.AppendChild(revisionNode);
        }

        /// <summary>
        /// Дополнительные сведения об участниках факта хозяйственной жизни, основаниях и обстоятельствах его проведения
        /// </summary>
        /// <remarks>
        /// ДопСвФХЖ1
        /// </remarks>
        private static void AddAdditionalInfo(XmlDocument xmlDocument, XmlElement parentElement, SellerUniversalTransferDocument dataContract)
        {
            if (!String.IsNullOrEmpty(dataContract.GovernmentContractInfo)
                || dataContract.CurrencyRate > 0
                || !String.IsNullOrEmpty(dataContract.CurrencyName)
                || dataContract.FactorInfo != null
                || dataContract.SellerInfoCircumPublicProc != null
                || dataContract.MainAssignMonetaryClaim != null
                || dataContract.InvoiceFormationType != InvoiceFormationType.NotSpecified)
            {
                var economicNode = xmlDocument.CreateElement("ДопСвФХЖ1");
                if (!String.IsNullOrEmpty(dataContract.GovernmentContractInfo))
                    economicNode.SetAttribute("ИдГосКон", dataContract.GovernmentContractInfo);

                if (!String.IsNullOrEmpty(dataContract.CurrencyName))
                    economicNode.SetAttribute("НаимОКВ", dataContract.CurrencyName);

                if (dataContract.CurrencyRate > 0)
                    economicNode.SetAttribute("КурсВал", dataContract.CurrencyRate.Value.ToString("0.00##", CultureInfo.InvariantCulture));

                if (dataContract.InvoiceFormationType != InvoiceFormationType.NotSpecified)
                    economicNode.SetAttribute("ОбстФормСЧФ", ((int)dataContract.InvoiceFormationType).ToString());

                if (dataContract.SellerInfoCircumPublicProc != null)
                {
                    var circumPublicProcNode = xmlDocument.CreateElement("ИнфПродГосЗакКазн");
                    circumPublicProcNode.SetAttribute("ДатаГосКонт", dataContract.SellerInfoCircumPublicProc.DateStateContract.ToString("dd.MM.yyyy"));
                    circumPublicProcNode.SetAttribute("НомерГосКонт", dataContract.SellerInfoCircumPublicProc.NumberStateContract);

                    if (!String.IsNullOrEmpty(dataContract.SellerInfoCircumPublicProc.PersonalAccountSeller))
                        circumPublicProcNode.SetAttribute("ЛицСчетПрод", dataContract.SellerInfoCircumPublicProc.PersonalAccountSeller);

                    if (!String.IsNullOrEmpty(dataContract.SellerInfoCircumPublicProc.SellerBudgetClassCode))
                        circumPublicProcNode.SetAttribute("КодПродБюджКласс", dataContract.SellerInfoCircumPublicProc.SellerBudgetClassCode);

                    if (!String.IsNullOrEmpty(dataContract.SellerInfoCircumPublicProc.SellerTargetCode))
                        circumPublicProcNode.SetAttribute("КодЦелиПрод", dataContract.SellerInfoCircumPublicProc.SellerTargetCode);

                    if (!String.IsNullOrEmpty(dataContract.SellerInfoCircumPublicProc.SellerTreasuryCode))
                        circumPublicProcNode.SetAttribute("КодКазначПрод", dataContract.SellerInfoCircumPublicProc.SellerTreasuryCode);

                    if (!String.IsNullOrEmpty(dataContract.SellerInfoCircumPublicProc.SellerTreasuryName))
                        circumPublicProcNode.SetAttribute("НаимКазначПрод", dataContract.SellerInfoCircumPublicProc.SellerTreasuryName);

                    economicNode.AppendChild(circumPublicProcNode);
                }

                if (dataContract.FactorInfo != null)
                {
                    AddFirmInformation(xmlDocument, economicNode, dataContract.FactorInfo, "СвФактор");
                }

                if (dataContract.MainAssignMonetaryClaim != null)
                {
                    var monetaryClaimNode = xmlDocument.CreateElement("ОснУстДенТреб");
                    monetaryClaimNode.SetAttribute("НаимОсн", dataContract.MainAssignMonetaryClaim.DocumentName);

                    if (!String.IsNullOrEmpty(dataContract.MainAssignMonetaryClaim.DocumentNumber))
                        monetaryClaimNode.SetAttribute("НомОсн", dataContract.MainAssignMonetaryClaim.DocumentNumber);

                    if (dataContract.MainAssignMonetaryClaim.DocumentDate != null)
                        monetaryClaimNode.SetAttribute("ДатаОсн", dataContract.MainAssignMonetaryClaim.DocumentDate.Value.ToString("dd.MM.yyyy"));

                    if (!String.IsNullOrEmpty(dataContract.MainAssignMonetaryClaim.DocumentInfo))
                        monetaryClaimNode.SetAttribute("ДопСвОсн", dataContract.MainAssignMonetaryClaim.DocumentInfo);

                    if (!String.IsNullOrEmpty(dataContract.MainAssignMonetaryClaim.DocumentId))
                        monetaryClaimNode.SetAttribute("ИдентОсн", dataContract.MainAssignMonetaryClaim.DocumentId);

                    economicNode.AppendChild(monetaryClaimNode);
                }

                parentElement.AppendChild(economicNode);
            }
        }

        /// <summary>
        /// Сведения о платежно-расчетном документе (строка 5 счета-фактуры)
        /// </summary>
        /// <remarks>
        /// СвПРД
        /// </remarks>
        private static void AddPaymentDocumentInfo(XmlDocument xmlDocument, XmlElement parentElement, List<PaymentDocumentInfo> paymentDocumentInfoList)
        {
            if (paymentDocumentInfoList == null || paymentDocumentInfoList.Count == 0)
                return;

            foreach (var paymentDocumentInfo in paymentDocumentInfoList)
            {
                var paymentDocumentInfoNode = xmlDocument.CreateElement("СвПРД"); // Сведения о платежно-расчетном документе (строка 5 счета-фактуры)
                paymentDocumentInfoNode.SetAttribute("НомерПРД", paymentDocumentInfo.Number); // Номер платежно-расчетного документа
                paymentDocumentInfoNode.SetAttribute("ДатаПРД", paymentDocumentInfo.Date.ToString("dd.MM.yyyy")); // Дата составления платежно-расчетного документа

                if (paymentDocumentInfo.Total != null)
                    paymentDocumentInfoNode.SetAttribute("СуммаПРД", paymentDocumentInfo.Total.Value.ToString("0.00", CultureInfo.InvariantCulture)); // Сумма

                parentElement.AppendChild(paymentDocumentInfoNode);
            }
        }

        private static string GetEconomicEntityName(Organization firm)
        {
            if (firm.OrganizationIdentificationInfo.LegalPerson is LegalPerson legalPerson)
            {
                if (!String.IsNullOrEmpty(legalPerson.Inn)
                    && !String.IsNullOrEmpty(legalPerson.Kpp))
                {
                    return $"{legalPerson.Name}, ИНН/КПП: {legalPerson.Inn}/{legalPerson.Kpp}";
                }
                else if (!String.IsNullOrEmpty(legalPerson.Inn))
                {
                    return $"{legalPerson.Name}, ИНН: {legalPerson.Inn}";
                }
                else
                {
                    return legalPerson.Name;
                }
            }
            else if (firm.OrganizationIdentificationInfo.IndividualEntrepreneur is IndividualEntrepreneur individualEntrepreneur)
            {
                var indName = $"ИП {individualEntrepreneur.GetFullName()}";
                if (!String.IsNullOrEmpty(individualEntrepreneur.Inn))
                {
                    return $"{indName}, ИНН: {individualEntrepreneur.Inn}";
                }
                else
                {
                    return indName;
                }
            }
            else if (firm.OrganizationIdentificationInfo.PhysicalPerson is PhysicalPerson physicalPerson)
            {
                if (!String.IsNullOrEmpty(physicalPerson.Inn))
                {
                    return $"{physicalPerson.GetFullName()}, ИНН: {physicalPerson.Inn}";
                }
                else
                {
                    return physicalPerson.GetFullName();
                }
            }
            else if (firm.OrganizationIdentificationInfo.ForeignPerson is ForeignPerson foreignPerson)
            {
                return foreignPerson.Name;
            }
            else
            {
                throw new ArgumentNullException("Не указаны идентификационные сведения организации.");
            }
        }

        private static void AddSameFirmInformation(XmlDocument xmlDocument, XmlElement parentElement, string elementName)
        {
            var sameFirmNode = xmlDocument.CreateElement(elementName);
            var sameFirmInfoNode = xmlDocument.CreateElement("ОнЖе");
            sameFirmInfoNode.InnerText = "он же";
            sameFirmNode.AppendChild(sameFirmInfoNode);
            parentElement.AppendChild(sameFirmNode);
        }

        private static void AddFirmInformation(XmlDocument xmlDocument, XmlElement parentElement, Organization firm, string elementName)
        {
            var firmNode = xmlDocument.CreateElement(elementName); // Сведения о продавце (строки 2, 2а, 2б счета-фактуры)
            if (!String.IsNullOrEmpty(firm.Okpo))
                firmNode.SetAttribute("ОКПО", firm.Okpo);

            if (!String.IsNullOrEmpty(firm.Department))
                firmNode.SetAttribute("СтруктПодр", firm.Department); // Структурное подразделение

            if (!String.IsNullOrEmpty(firm.OrganizationAdditionalInfo))
                firmNode.SetAttribute("ИнфДляУчаст", firm.OrganizationAdditionalInfo); // Информация для участника документооборота

            if (!String.IsNullOrEmpty(firm.ShortName))
                firmNode.SetAttribute("КраткНазв", firm.ShortName);

            var firmIdNode = xmlDocument.CreateElement("ИдСв"); // Идентификационные сведения
            if (firm.OrganizationIdentificationInfo.LegalPerson is LegalPerson legalPerson)
            {
                var firmInfoNode = xmlDocument.CreateElement("СвЮЛУч"); // Сведения о юридическом лице, состоящем на учете в налоговых органах
                firmInfoNode.SetAttribute("НаимОрг", legalPerson.Name);

                if (legalPerson.IsHyphenInn)
                    firmInfoNode.SetAttribute("ДефИННЮЛ", "-");
                else
                    firmInfoNode.SetAttribute("ИННЮЛ", legalPerson.Inn);

                firmInfoNode.SetAttribute("КПП", legalPerson.Kpp);

                firmIdNode.AppendChild(firmInfoNode);
                firmNode.AppendChild(firmIdNode);
            }
            else if (firm.OrganizationIdentificationInfo.IndividualEntrepreneur is IndividualEntrepreneur individualEntrepreneur)
            {
                var firmInfoNode = xmlDocument.CreateElement("СвИП"); // Сведения об индивидуальном предпринимателе

                if (individualEntrepreneur.IsHyphenInn)
                    firmInfoNode.SetAttribute("ДефИННФЛ", "-");
                else
                    firmInfoNode.SetAttribute("ИННФЛ", individualEntrepreneur.Inn);

                if (!String.IsNullOrEmpty(individualEntrepreneur.IndividualEntrepreneurRegistrationCertificate))
                    firmInfoNode.SetAttribute("СвГосРегИП", individualEntrepreneur.IndividualEntrepreneurRegistrationCertificate); // Реквизиты свидетельства о государственной регистрации индивидуального предпринимателя

                if (!String.IsNullOrEmpty(individualEntrepreneur.OtherInfo))
                    firmInfoNode.SetAttribute("ИныеСвед", individualEntrepreneur.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                AddPersonName(xmlDocument, individualEntrepreneur, firmInfoNode);

                firmIdNode.AppendChild(firmInfoNode);
                firmNode.AppendChild(firmIdNode);
            }
            else if (firm.OrganizationIdentificationInfo.PhysicalPerson is PhysicalPerson physicalPerson)
            {
                var firmInfoNode = xmlDocument.CreateElement("СвФЛУчастФХЖ"); // Сведения о физическом лице
                firmInfoNode.SetAttribute("ИННФЛ", physicalPerson.Inn);

                if (!String.IsNullOrEmpty(physicalPerson.IndividualEntrepreneurRegistrationCertificate))
                    firmInfoNode.SetAttribute("ГосРегИПВыдДов", physicalPerson.IndividualEntrepreneurRegistrationCertificate); // Реквизиты свидетельства о государственной регистрации индивидуального предпринимателя, выдавшего доверенность физическому лицу на подписание счета-фактуры

                if (!String.IsNullOrEmpty(physicalPerson.OtherInfo))
                    firmInfoNode.SetAttribute("ИныеСвед", physicalPerson.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                AddPersonName(xmlDocument, physicalPerson, firmInfoNode);

                firmIdNode.AppendChild(firmInfoNode);
                firmNode.AppendChild(firmIdNode);
            }
            else if (firm.OrganizationIdentificationInfo.ForeignPerson is ForeignPerson foreignPerson)
            {
                var firmInfoNode = xmlDocument.CreateElement("СвИнНеУч"); // Сведения об иностранном лице, не состоящем на учете в налоговых органах
                firmInfoNode.SetAttribute("НаимОрг", foreignPerson.Name);

                if (!String.IsNullOrEmpty(foreignPerson.LegalEntityId))
                    firmInfoNode.SetAttribute("Идентиф", foreignPerson.LegalEntityId); // Идентификатор юридического лица

                if (!String.IsNullOrEmpty(foreignPerson.OtherInfo))
                    firmInfoNode.SetAttribute("ИныеСвед", foreignPerson.OtherInfo); // Например, указывается страна при отсутствии КодСтр

                firmIdNode.AppendChild(firmInfoNode);
                firmNode.AppendChild(firmIdNode);
            }
            else
            {
                throw new ArgumentNullException("Не указаны идентификационные сведения организации.");
            }

            AddAddressInformation(xmlDocument, firm.Address, firmNode);

            AddContactInformation(xmlDocument, firm.Contact, firmNode);

            AddBankDetailsInformation(xmlDocument, firm.BankAccountDetails, firmNode);

            parentElement.AppendChild(firmNode);
        }

        private static void AddAddressInformation(XmlDocument xmlDocument, Address address, XmlElement parentElement)
        {
            if (address.RussianAddress is RussianAddress russianAddress)
            {
                var russianFirmAddressNode = xmlDocument.CreateElement("Адрес");
                var russianFirmAddressInfoNode = xmlDocument.CreateElement("АдрРФ");
                if (!String.IsNullOrEmpty(russianAddress.ZipCode))
                    russianFirmAddressInfoNode.SetAttribute("Индекс", russianAddress.ZipCode);

                russianFirmAddressInfoNode.SetAttribute("КодРегион", russianAddress.RegionCode);

                if (!String.IsNullOrEmpty(russianAddress.Street))
                    russianFirmAddressInfoNode.SetAttribute("Улица", russianAddress.Street);

                if (!String.IsNullOrEmpty(russianAddress.Building))
                    russianFirmAddressInfoNode.SetAttribute("Дом", russianAddress.Building);

                if (!String.IsNullOrEmpty(russianAddress.Block))
                    russianFirmAddressInfoNode.SetAttribute("Корпус", russianAddress.Block);

                if (!String.IsNullOrEmpty(russianAddress.Apartment))
                    russianFirmAddressInfoNode.SetAttribute("Кварт", russianAddress.Apartment);

                if (!String.IsNullOrEmpty(russianAddress.City))
                    russianFirmAddressInfoNode.SetAttribute("Город", russianAddress.City);

                if (!String.IsNullOrEmpty(russianAddress.Territory))
                    russianFirmAddressInfoNode.SetAttribute("Район", russianAddress.Territory);

                if (!String.IsNullOrEmpty(russianAddress.Locality))
                    russianFirmAddressInfoNode.SetAttribute("НаселПункт", russianAddress.Locality);

                russianFirmAddressNode.AppendChild(russianFirmAddressInfoNode);
                parentElement.AppendChild(russianFirmAddressNode);
            }
            else if (address.ForeignAddress is ForeignAddress foreignAddress)
            {
                var foreignFirmAddressNode = xmlDocument.CreateElement("Адрес");
                var foreignFirmAddressInfoNode = xmlDocument.CreateElement("АдрИнф");

                foreignFirmAddressInfoNode.SetAttribute("КодСтр", foreignAddress.CountryCode);
                foreignFirmAddressInfoNode.SetAttribute("Адрес", foreignAddress.Address);

                foreignFirmAddressNode.AppendChild(foreignFirmAddressInfoNode);
                parentElement.AppendChild(foreignFirmAddressNode);
            }
            else if (address.AddressCode is AddressCode addressCode)
            {
                var codeFirmAddressNode = xmlDocument.CreateElement("Адрес");
                var codeFirmAddressInfoNode = xmlDocument.CreateElement("КодГАР");

                codeFirmAddressInfoNode.InnerText = addressCode.UniqueCode;

                codeFirmAddressNode.AppendChild(codeFirmAddressInfoNode);
                parentElement.AppendChild(codeFirmAddressNode);
            }
            else
            {

            }
        }

        private static void AddContactInformation(XmlDocument xmlDocument, Contact contact, XmlElement firmNode)
        {
            if (contact != null)
            {
                var contactNode = xmlDocument.CreateElement("Контакт"); // Контактные данные
                if (!String.IsNullOrEmpty(contact.Phone))
                    contactNode.SetAttribute("Тлф", contact.Phone); // Номер контактного телефона/факс

                if (!String.IsNullOrEmpty(contact.Email))
                    contactNode.SetAttribute("ЭлПочта", contact.Email); // Адрес электронной почты

                firmNode.AppendChild(contactNode);
            }
        }

        private static void AddBankDetailsInformation(XmlDocument xmlDocument, BankAccountDetails bankAccountDetails, XmlElement parentElement)
        {
            if (bankAccountDetails != null)
            {
                var bankNode = xmlDocument.CreateElement("БанкРекв"); // Банковские реквизиты
                if (!String.IsNullOrEmpty(bankAccountDetails.BankAccountNumber))
                    bankNode.SetAttribute("НомерСчета", bankAccountDetails.BankAccountNumber); // Номер банковского счета

                var bankDetails = bankAccountDetails.BankDetails;
                if (bankDetails != null)
                {
                    var bankInfoNode = xmlDocument.CreateElement("СвБанк"); // Сведения о банке
                    if (!String.IsNullOrEmpty(bankDetails.BankName))
                        bankInfoNode.SetAttribute("НаимБанк", bankDetails.BankName); // Наименование банка

                    if (!String.IsNullOrEmpty(bankDetails.BankId))
                        bankInfoNode.SetAttribute("БИК", bankDetails.BankId); // БИК

                    if (!String.IsNullOrEmpty(bankDetails.CorrespondentAccount))
                        bankInfoNode.SetAttribute("КорСчет", bankDetails.CorrespondentAccount); // Корреспондентский счет банка

                    bankNode.AppendChild(bankInfoNode);
                }

                parentElement.AppendChild(bankNode);
            }
        }

        /// <summary>
        /// ИнфПолФХЖ2
        /// </summary>
        private static void AddOtherEconomicInfo(XmlDocument xmlDocument, XmlElement parentElement, List<OtherEconomicInfoItem> otherEconomicInfoItemList, string elementName)
        {
            if (otherEconomicInfoItemList == null || otherEconomicInfoItemList.Count == 0)
                return;

            foreach (var item in otherEconomicInfoItemList)
            {
                var additionalInfoNode = xmlDocument.CreateElement(elementName);
                if (!String.IsNullOrEmpty(item.Id))
                    additionalInfoNode.SetAttribute("Идентиф", item.Id); // Идентификатор

                if (!String.IsNullOrEmpty(item.Value))
                    additionalInfoNode.SetAttribute("Значен", item.Value); // Значение

                parentElement.AppendChild(additionalInfoNode);
            }
        }

        /// <summary>
        /// Информационное поле факта хозяйственной жизни (1, 2, 4)
        /// </summary>
        private static void AddOtherEconomicInfo(XmlDocument xmlDocument, XmlElement parentElement, OtherEconomicInfo otherEconomicInfo, string elementName)
        {
            if (otherEconomicInfo == null)
                return;

            var additionalInfoNode = xmlDocument.CreateElement(elementName); // Информационное поле факта хозяйственной жизни
            if (!String.IsNullOrEmpty(otherEconomicInfo.InfoFileId))
                additionalInfoNode.SetAttribute("ИдФайлИнфПол", otherEconomicInfo.InfoFileId); // Идентификатор файла информационного поля

            if (otherEconomicInfo.Items != null)
            {
                foreach (var item in otherEconomicInfo.Items)
                {
                    var additionalInfoTextNode = xmlDocument.CreateElement("ТекстИнф"); // Текстовая информация
                    if (!String.IsNullOrEmpty(item.Id))
                        additionalInfoTextNode.SetAttribute("Идентиф", item.Id); // Идентификатор

                    if (!String.IsNullOrEmpty(item.Value))
                        additionalInfoTextNode.SetAttribute("Значен", item.Value); // Значение

                    additionalInfoNode.AppendChild(additionalInfoTextNode);
                }
            }

            parentElement.AppendChild(additionalInfoNode);
        }

        /// <summary>
        /// Сведения таблицы счета-фактуры
        /// (содержание факта хозяйственной жизни 2 - наименование и другая информация об отгруженных товарах
        /// (выполненных работах, оказанных услугах), о переданных имущественных правах
        /// </summary>
        /// <remarks>
        /// ТаблСчФакт
        /// </remarks>
        private static void AddObjectiveItems(XmlDocument xmlDocument, XmlElement parentElement, SellerUniversalTransferDocument dataContract)
        {
            var objectiveNode = xmlDocument.CreateElement("ТаблСчФакт");
            for (var i = 0; i < dataContract.Items.Count; i++)
            {
                var item = dataContract.Items[i];
                var itemNode = xmlDocument.CreateElement("СведТов");
                itemNode.SetAttribute("НомСтр", (i + 1).ToString()); // Номер строки таблицы
                itemNode.SetAttribute("НаимТов", item.ProductName); // Наименование товара

                if (!String.IsNullOrEmpty(item.UnitCode))
                    itemNode.SetAttribute("ОКЕИ_Тов", item.UnitCode); // Код единицы измерения (графа 2 счета-фактуры)
                else if (item.IsHyphenUnitCode)
                    itemNode.SetAttribute("ДефОКЕИ_Тов", "-"); // Код единицы измерения (графа 2 счета-фактуры при составлении документа с Функция=СЧФ или Функция=СЧФДОП при отсутствии данных и для документа с Функция=СЧФ, выставляемом при получении оплаты, частичной оплаты в счет предстоящих поставок товаров (выполнения работ, оказания услуг), передачи имущественных прав)

                itemNode.SetAttribute("КолТов", item.Quantity.ToString(quantityToStringPattern, CultureInfo.InvariantCulture)); // Количество (объем) (графа 3 счета - фактуры)
                itemNode.SetAttribute("ЦенаТов", item.Price.ToString("0.00", CultureInfo.InvariantCulture)); // Цена (тариф) за единицу измерения (графа 4 счета-фактуры)
                itemNode.SetAttribute("СтТовБезНДС", item.SumWithoutVat.ToString("0.00", CultureInfo.InvariantCulture)); // Стоимость товаров (работ, услуг), имущественных прав без налога - всего (графа 5 счета-фактуры)
                itemNode.SetAttribute("НалСт", item.TaxRate.GetTaxRateName()); // Налоговая ставка (графа 7 счета-фактуры)

                if (item.Sum != null)
                    itemNode.SetAttribute("СтТовУчНал", item.Sum.Value.ToString("0.00", CultureInfo.InvariantCulture)); // Стоимость товаров(работ, услуг), имущественных прав с налогом - всего (графа 9 счетафактуры)
                else if (item.IsHyphenSum)
                    itemNode.SetAttribute("ДефСтТовУчНал", "-"); // Стоимость товаров (работ, услуг), имущественных прав с налогом - всего (графа 9 счета-фактуры) при составлении документа с Функция=СЧФ или Функция=СЧФДОП при отсутствии показателя

                // Акциз
                AddExciseValue(xmlDocument, item, itemNode);

                var taxNode = xmlDocument.CreateElement("СумНал");// Сумма налога, предъявляемая покупателю (графа 8 счета-фактуры)
                AddVatValue(xmlDocument, item.TaxRate == TaxRate.WithoutVat, item.IsHyphenVat, item.Vat, taxNode);

                itemNode.AppendChild(taxNode);

                // СвТД
                AddCustomDeclarationsInformation(xmlDocument, item, itemNode);

                // ДопСведТов
                AddAdditionalInfo(xmlDocument, item, itemNode);

                // ИнфПолФХЖ2
                AddOtherEconomicInfo(xmlDocument, itemNode, item.OtherEconomicInfoItemList, "ИнфПолФХЖ2");

                objectiveNode.AppendChild(itemNode);
            }

            // ВсегоОпл
            AddObjectiveTotal(xmlDocument, dataContract.Items, objectiveNode);

            parentElement.AppendChild(objectiveNode);
        }

        /// <summary>
        /// Реквизиты строки "Всего к оплате"
        /// </summary>
        private static void AddObjectiveTotal(XmlDocument xmlDocument, List<InvoiceItem> items, XmlElement objectiveNode)
        {
            var totalPaidNode = xmlDocument.CreateElement("ВсегоОпл"); // Реквизиты строки «Всего к оплате»
            totalPaidNode.SetAttribute("СтТовБезНДСВсего", items.Sum(x => x.SumWithoutVat).ToString("0.00", CultureInfo.InvariantCulture)); // Всего к оплате, Стоимость товаров (работ, услуг), имущественных прав без налога - всего(строка «Всего к оплате»/ графа 5 счета - фактуры)
            var isHyphenTotal = !items.Any(x => x.Sum != null);
            if (!isHyphenTotal)
                totalPaidNode.SetAttribute("СтТовУчНалВсего", items.Sum(x => (x.Sum ?? 0M)).ToString("0.00", CultureInfo.InvariantCulture)); // Всего к оплате, Стоимость товаров(работ, услуг), имущественных прав с налогом - всего (строка «Всего к оплате»/ графа 9 счета - фактуры)
            else if (isHyphenTotal)
                totalPaidNode.SetAttribute("ДефСтТовУчНалВсего", "-"); // Всего к оплате, Стоимость товаров (работ, услуг), имущественных прав с налогом - всего (строка "Всего к оплате"/графа 9 счета-фактуры) при отсутствии показателя

            var totalTaxNode = xmlDocument.CreateElement("СумНалВсего"); // Всего к оплате, Сумма налога, предъявляемая покупателю (строка «Всего к оплате»/ графа 8 счета-фактуры)
            var isWithoutVat = !items.Any(x => x.TaxRate != TaxRate.WithoutVat);
            var isHyphenVat = !items.Any(x => !x.IsHyphenVat);
            AddVatValue(xmlDocument, isWithoutVat, isHyphenVat, items.Sum(x => x.Vat), totalTaxNode);
            totalPaidNode.AppendChild(totalTaxNode);

            var totalQuantity = items.Sum(x => x.Quantity);
            if (totalQuantity > 0)
            {
                var totalQuantityNode = xmlDocument.CreateElement("КолНеттоВс");
                totalQuantityNode.InnerText = totalQuantity.ToString(quantityToStringPattern, CultureInfo.InvariantCulture);

                totalPaidNode.AppendChild(totalQuantityNode);
            }

            objectiveNode.AppendChild(totalPaidNode);
        }

        /// <summary>
        /// ДопСведТов
        /// </summary>
        private static void AddAdditionalInfo(XmlDocument xmlDocument, InvoiceItem item, XmlElement itemNode)
        {
            var additionalInfo = item.AdditionalInfo;
            if (additionalInfo != null)
            {
                var additionalInfoNode = xmlDocument.CreateElement("ДопСведТов"); // Дополнительные сведения об отгруженных товарах (выполненных работах, оказанных услугах), переданных имущественных правах

                if (additionalInfo.Type != InvoiceItemType.NotSpecified)
                    additionalInfoNode.SetAttribute("ПрТовРаб", ((int)additionalInfo.Type).ToString()); // Признак Товар/Работа/Услуга/Право/Иное

                if (!String.IsNullOrEmpty(additionalInfo.AdditionalTypeInfo))
                    additionalInfoNode.SetAttribute("ДопПризн", additionalInfo.AdditionalTypeInfo); // Дополнительная информация о признаке

                if (!String.IsNullOrEmpty(additionalInfo.UnitName))
                    additionalInfoNode.SetAttribute("НаимЕдИзм", additionalInfo.UnitName); // Наименование единицы измерения (условное обозначение национальное, графа 2а счета-фактуры)

                if (!String.IsNullOrEmpty(additionalInfo.CountryName))
                    additionalInfoNode.SetAttribute("КрНаимСтрПр", additionalInfo.CountryName); // Краткое наименование страны происхождения товара (графа 10а счетафактуры)/страна регистрации производителя товара

                if (additionalInfo.ItemToRelease != null)
                    additionalInfoNode.SetAttribute("НадлОтп", additionalInfo.ItemToRelease.Value.ToString("0.#######", CultureInfo.InvariantCulture)); // Заказанное количество (количество надлежит отпустить)

                if (!String.IsNullOrEmpty(additionalInfo.Characteristic))
                    additionalInfoNode.SetAttribute("ХарактерТов", additionalInfo.Characteristic); // Характеристика/описание товара (в том числе графа 1 счета-фактуры) 

                if (!String.IsNullOrEmpty(additionalInfo.Kind))
                    additionalInfoNode.SetAttribute("СортТов", additionalInfo.Kind); // Сорт товара

                if (!String.IsNullOrEmpty(additionalInfo.Article))
                    additionalInfoNode.SetAttribute("АртикулТов", additionalInfo.Article);// Артикул товара (в том числе графа 1 счета-фактуры)

                if (!String.IsNullOrEmpty(additionalInfo.Code))
                    additionalInfoNode.SetAttribute("КодТов", additionalInfo.Code); // Код товара (в том числе графа 1 счета-фактуры)

                if (!String.IsNullOrEmpty(additionalInfo.CatalogCode))
                    additionalInfoNode.SetAttribute("КодКат", additionalInfo.CatalogCode); // Код каталога

                if (!String.IsNullOrEmpty(additionalInfo.FeaccCode))
                    additionalInfoNode.SetAttribute("КодВидТов", additionalInfo.FeaccCode); // Код вида товара

                // СведПрослеж
                AddItemTracingInfo(xmlDocument, additionalInfo.ItemTracingInfoList, additionalInfoNode);

                // НомСредИдентТов
                AddItemIdentificationNumberInfo(xmlDocument, additionalInfo.ItemIdentificationNumberList, additionalInfoNode);

                itemNode.AppendChild(additionalInfoNode);
            }
        }

        /// <summary>
        /// СведПрослеж
        /// </summary>
        private static void AddItemTracingInfo(XmlDocument xmlDocument, List<InvoiceItemTracingInfo> itemTracingInfoList, XmlElement parentElement)
        {
            if (itemTracingInfoList?.Count > 0)
            {
                foreach (var item in itemTracingInfoList)
                {
                    var itemNode = xmlDocument.CreateElement("СведПрослеж"); // Сведения о товаре, подлежащем прослеживаемости
                    itemNode.SetAttribute("НомТовПрослеж", item.RegNumberUnit); // Регистрационный номер партии товаров
                    itemNode.SetAttribute("ЕдИзмПрослеж", item.UnitCode); // Единица количественного учета товара, используемая в целях осуществления прослеживаемости
                    if (!String.IsNullOrEmpty(item.UnitName))
                        itemNode.SetAttribute("НаимЕдИзмПрослеж", item.UnitName); // Наименование единицы количественного учета товара, используемой в целях осуществления прослеживаемости.

                    itemNode.SetAttribute("КолВЕдПрослеж", item.Quantity.ToString(quantityToStringPattern, CultureInfo.InvariantCulture)); // Количество товара в единицах измерения прослеживаемого товара
                    if (!String.IsNullOrEmpty(item.AdditionalInfo))
                        itemNode.SetAttribute("ДопПрослеж", item.AdditionalInfo); // Дополнительный показатель для идентификации товаров, подлежащих прослеживаемости

                    parentElement.AppendChild(itemNode);
                }
            }
        }

        /// <summary>
        /// НомСредИдентТов
        /// </summary>
        private static void AddItemIdentificationNumberInfo(XmlDocument xmlDocument, List<InvoiceItemIdentificationNumber> itemIdentificationNumberList, XmlElement parentElement)
        {
            if (itemIdentificationNumberList?.Count > 0)
            {
                foreach (var identificationNumber in itemIdentificationNumberList)
                {
                    var rfidNode = xmlDocument.CreateElement("НомСредИдентТов"); // Номер средств идентификации товаров
                    if (!String.IsNullOrEmpty(identificationNumber.PackageId))
                        rfidNode.SetAttribute("ИдентТрансУпак", identificationNumber.PackageId); // Уникальный идентификатор транспортной упаковки

                    if (identificationNumber.MarkItems != null) // Контрольный идентификационный знак
                    {
                        foreach (var item in identificationNumber.MarkItems)
                        {
                            var itemInfoNode = xmlDocument.CreateElement("КИЗ");
                            itemInfoNode.InnerText = item;
                            rfidNode.AppendChild(itemInfoNode);
                        }
                    }
                    else if (identificationNumber.SecondaryPackageItems != null) // Уникальный идентификатор вторичной (потребительской)/третичной (заводской, транспортной) упаковки
                    {
                        foreach (var item in identificationNumber.SecondaryPackageItems)
                        {
                            var itemInfoNode = xmlDocument.CreateElement("НомУпак");
                            itemInfoNode.InnerText = item;
                            rfidNode.AppendChild(itemInfoNode);
                        }
                    }

                    parentElement.AppendChild(rfidNode);
                }
            }
        }

        /// <summary>
        /// Акциз
        /// </summary>
        private static void AddExciseValue(XmlDocument xmlDocument, InvoiceItem item, XmlElement itemNode)
        {
            var exciseNode = xmlDocument.CreateElement("Акциз"); // В том числе сумма акциза (графа 6 счета-фактуры)
            if (item.Excise == null)
            {
                var exciseInfoNode = xmlDocument.CreateElement("БезАкциз");
                exciseInfoNode.InnerText = "без акциза";
                exciseNode.AppendChild(exciseInfoNode);
                itemNode.AppendChild(exciseNode);
            }
            else
            {
                var exciseInfoNode = xmlDocument.CreateElement("СумАкциз");
                exciseInfoNode.InnerText = item.Excise.Value.ToString("0.00", CultureInfo.InvariantCulture);
                exciseNode.AppendChild(exciseInfoNode);
                itemNode.AppendChild(exciseNode);
            }
        }

        /// <summary>
        /// СвТД
        /// </summary>
        private static void AddCustomDeclarationsInformation(XmlDocument xmlDocument, InvoiceItem item, XmlElement itemNode)
        {
            if (item.CustomsDeclarationList?.Count > 0)
            {
                foreach (var customDeclaration in item.CustomsDeclarationList)
                {
                    var declaration = xmlDocument.CreateElement("СвТД"); // Сведения о таможенной декларации

                    if (!String.IsNullOrEmpty(customDeclaration.CountryCode))
                        declaration.SetAttribute("КодПроисх", customDeclaration.CountryCode); // Цифровой код страны происхождения товара (Графа 10 счета-фактуры)
                    else if (customDeclaration.IsHyphenCountryCode)
                        declaration.SetAttribute("ДефКодПроисх", "-"); // Цифровой код страны происхождения товара (Графа 10 счета-фактуры; для документа с Функция=СЧФ, выставляемом при получении оплаты, частичной оплаты в счет предстоящих поставок товаров (выполнения работ, оказания услуг), передачи имущественных прав

                    if (!String.IsNullOrEmpty(customDeclaration.DeclarationNumber))
                        declaration.SetAttribute("НомерТД", customDeclaration.DeclarationNumber); // Регистрационный номер таможенной декларации (номер декларации на товары; графа 11 счета-фактуры)

                    itemNode.AppendChild(declaration);
                }
            }
        }

        private static void AddVatValue(XmlDocument xmlDocument, bool isWithoutVat, bool isHyphenVat, decimal? vat, XmlElement parentTaxNode)
        {
            if (isWithoutVat)
            {
                var withoutVatNode = xmlDocument.CreateElement("БезНДС");
                withoutVatNode.InnerText = "без НДС";

                parentTaxNode.AppendChild(withoutVatNode);
            }
            else if (isHyphenVat)
            {
                var hyphenVatNode = xmlDocument.CreateElement("ДефНДС");
                hyphenVatNode.InnerText = "-";

                parentTaxNode.AppendChild(hyphenVatNode);
            }
            else
            {
                if (vat == null)
                    throw new ArgumentNullException("Не указана сумма НДС!");

                var taxInfoNode = xmlDocument.CreateElement("СумНал");
                taxInfoNode.InnerText = vat.Value.ToString("0.00", CultureInfo.InvariantCulture);

                parentTaxNode.AppendChild(taxInfoNode);
            }
        }

        /// <summary>
        /// Содержание факта хозяйственной жизни 3 - сведения о факте отгрузки товаров (выполнения работ),
        /// передачи имущественных прав (о предъявлении оказанных услуг)
        /// </summary>
        /// <remarks>
        /// СвПродПер
        /// </remarks>
        private static void AddTransferInfo(XmlDocument xmlDocument, XmlElement parentElement, SellerUniversalTransferDocument dataContract)
        {
            var transferInfo = dataContract.TransferInfo;
            if (transferInfo == null)
                return;

            var transferNode = xmlDocument.CreateElement("СвПродПер"); // Содержание факта хозяйственной жизни 3 – сведения о факте отгрузки товаров (выполнения работ), передачи имущественных прав (о предъявлении оказанных услуг)

            var transferInfoNode = xmlDocument.CreateElement("СвПер"); // Сведения о передаче (сдаче) товаров (результатов работ), имущественных прав(о предъявлении оказанных услуг)
            transferInfoNode.SetAttribute("СодОпер", transferInfo.OperationName); // Содержание операции

            if (!String.IsNullOrEmpty(transferInfo.OperationType))
                transferInfoNode.SetAttribute("ВидОпер", transferInfo.OperationType); // Вид операции

            if (transferInfo.Date != null)
                transferInfoNode.SetAttribute("ДатаПер", transferInfo.Date.Value.ToString("dd.MM.yyyy")); // Дата отгрузки товаров (передачи результатов работ), передачи имущественных прав (предъявления оказанных услуг)

            if (transferInfo.StartDate != null)
                transferInfoNode.SetAttribute("ДатаНач", transferInfo.StartDate.Value.ToString("dd.MM.yyyy")); // Дата начала периода оказания услуг (выполнения работ, поставки товаров)

            if (transferInfo.EndDate != null)
                transferInfoNode.SetAttribute("ДатаОкон", transferInfo.EndDate.Value.ToString("dd.MM.yyyy")); // Дата окончания периода оказания услуг (выполнения работ, поставки товаров)


            foreach (var transferDoc in transferInfo.TransferDocuments)
            {
                var transferDocumentNode = xmlDocument.CreateElement("ОснПер"); // Основание отгрузки товаров (передачи результатов работ), передачи имущественных прав (предъявления оказанных услуг)
                transferDocumentNode.SetAttribute("НаимОсн", transferDoc.DocumentName); // Наименование документа - основания

                if (!String.IsNullOrEmpty(transferDoc.DocumentNumber))
                    transferDocumentNode.SetAttribute("НомОсн", transferDoc.DocumentNumber); // Номер документа - основания

                if (transferDoc.DocumentDate != null)
                    transferDocumentNode.SetAttribute("ДатаОсн", transferDoc.DocumentDate.Value.ToString("dd.MM.yyyy")); // Дата документа - основания

                if (!String.IsNullOrEmpty(transferDoc.DocumentInfo))
                    transferDocumentNode.SetAttribute("ДопСвОсн", transferDoc.DocumentInfo); // Дополнительные сведения

                if (!String.IsNullOrEmpty(transferDoc.DocumentId))
                    transferDocumentNode.SetAttribute("ИдентОсн", transferDoc.DocumentId); // Идентификатор документа-основания

                transferInfoNode.AppendChild(transferDocumentNode);
            }

            if (transferInfo.SenderPerson != null)
            {
                var sender = transferInfo.SenderPerson;

                var senderNode = xmlDocument.CreateElement("СвЛицПер"); // Сведения о лице, передавшем товар (груз)
                if (sender.Employee is SenderEmployee senderEmployee)
                {
                    var senderEmployeeNode = xmlDocument.CreateElement("РабОргПрод"); // Работник организации продавца
                    senderEmployeeNode.SetAttribute("Должность", senderEmployee.JobTitle); // Должность

                    if (!String.IsNullOrEmpty(senderEmployee.EmployeeInfo))
                        senderEmployeeNode.SetAttribute("ИныеСвед", senderEmployee.EmployeeInfo); // Иные сведения, идентифицирующие физическое лицо

                    if (!String.IsNullOrEmpty(senderEmployee.EmployeeBase))
                        senderEmployeeNode.SetAttribute("ОснПолн", senderEmployee.EmployeeBase); // Основание полномочий (доверия)

                    senderNode.AppendChild(senderEmployeeNode);

                    AddPersonName(xmlDocument, senderEmployee, senderEmployeeNode);
                }
                else if (sender.OtherIssuer is OtherIssuer senderOtherIssue)
                {
                    if (senderOtherIssue.OrganizationPerson is TransferOrganizationPerson organizationPerson)
                    {
                        var senderOtherIssueEmployeeNode = xmlDocument.CreateElement("ПредОргПер"); // Представитель организации, которой доверена отгрузка товаров
                        senderOtherIssueEmployeeNode.SetAttribute("Должность", organizationPerson.JobTitle); // Должность

                        if (!String.IsNullOrEmpty(organizationPerson.EmployeeInfo))
                            senderOtherIssueEmployeeNode.SetAttribute("ИныеСвед", organizationPerson.EmployeeInfo); // Иные сведения, идентифицирующие физическое лицо

                        senderOtherIssueEmployeeNode.SetAttribute("НаимОргПер", organizationPerson.OrganizationName); // Наименование организации

                        if (!String.IsNullOrEmpty(organizationPerson.OrganizationBase))
                            senderOtherIssueEmployeeNode.SetAttribute("ОснДоверОргПер", organizationPerson.OrganizationBase); // Основание, по которому организации доверена отгрузка товаров

                        if (!String.IsNullOrEmpty(organizationPerson.EmployeeBase))
                            senderOtherIssueEmployeeNode.SetAttribute("ОснПолнПредПер", organizationPerson.EmployeeBase); // Основание полномочий представителя организации на отгрузку товаров

                        senderNode.AppendChild(senderOtherIssueEmployeeNode);

                        AddPersonName(xmlDocument, organizationPerson, senderOtherIssueEmployeeNode);
                    }
                    else if (senderOtherIssue.PhysicalPerson is TransferPhysicalPerson physicalPerson)
                    {
                        var senderOtherIssuePersonNode = xmlDocument.CreateElement("ФЛПер"); // Представитель организации, которой доверена отгрузка товаров

                        if (!String.IsNullOrEmpty(physicalPerson.PersonInfo))
                            senderOtherIssuePersonNode.SetAttribute("ИныеСвед", physicalPerson.PersonInfo); // Иные сведения, идентифицирующие физическое лицо

                        if (!String.IsNullOrEmpty(physicalPerson.PersonBase))
                            senderOtherIssuePersonNode.SetAttribute("ОснДоверФЛ", physicalPerson.PersonBase); // Основание, по которому физическому лицу доверена отгрузка товаров

                        senderNode.AppendChild(senderOtherIssuePersonNode);

                        AddPersonName(xmlDocument, physicalPerson, senderOtherIssuePersonNode);
                    }
                    else
                    {
                        throw new ArgumentNullException("Не указаны сведения о ином лице, передавшем товар (ИнЛицо).");
                    }
                }
                else
                {
                    throw new ArgumentNullException("Не указаны сведения о лице, передавшем товар (СвЛицПер).");
                }

                transferInfoNode.AppendChild(senderNode);
            }

            var transferTextInfoNode = xmlDocument.CreateElement("ТранГруз"); // Транспортировка и груз
            if (transferInfo.Transportation != null)
            {
                if (!String.IsNullOrEmpty(transferInfo.Transportation.TransferTextInfo))
                    transferTextInfoNode.SetAttribute("СвТранГруз", transferInfo.Transportation.TransferTextInfo); // Сведения о транспортировке и грузе

                if (transferInfo.Transportation.Waybills?.Count > 0)
                {
                    foreach (var waybill in transferInfo.Transportation.Waybills)
                    {
                        var waybillNode = xmlDocument.CreateElement("ТранНакл"); // Транспортная накладная
                        waybillNode.SetAttribute("НомТранНакл", waybill.TransferDocumentNumber); // Номер транспортной накладной
                        waybillNode.SetAttribute("ДатаТранНакл", waybill.TransferDocumentDate.ToString("dd.MM.yyyy")); // Дата транспортной накладной

                        transferTextInfoNode.AppendChild(waybillNode);
                    }
                }

                if (transferInfo.Transportation.Carrier != null)
                {
                    AddFirmInformation(xmlDocument, transferTextInfoNode, transferInfo.Transportation.Carrier, "Перевозчик");
                }

                transferInfoNode.AppendChild(transferTextInfoNode);
            }

            if (transferInfo.CreatedThingInfo != null)
            {
                var createdThingInfo = transferInfo.CreatedThingInfo;
                var createdThingInfoNode = xmlDocument.CreateElement("СвПерВещи"); // Сведения о передаче вещи, изготовленной по договору подряда

                if (createdThingInfo.Date != null)
                    createdThingInfoNode.SetAttribute("ДатаПерВещ", createdThingInfo.Date.Value.ToString("dd.MM.yyyy")); // Дата передачи вещи, изготовленной по договору подряда

                if (!String.IsNullOrEmpty(createdThingInfo.Information))
                    createdThingInfoNode.SetAttribute("СвПерВещ", createdThingInfo.Information); // Сведения о передаче

                transferInfoNode.AppendChild(createdThingInfoNode);
            }

            transferNode.AppendChild(transferInfoNode);

            AddOtherEconomicInfo(xmlDocument, transferNode, transferInfo.OtherEconomicInfo, "ИнфПолФХЖ3");

            parentElement.AppendChild(transferNode);
        }

        private static void AddPersonName(XmlDocument xmlDocument, Person person, XmlElement parentNode)
        {
            var personNode = xmlDocument.CreateElement("ФИО");
            personNode.SetAttribute("Фамилия", person.Surname);
            personNode.SetAttribute("Имя", person.FirstName);

            if (!String.IsNullOrEmpty(person.Patronymic))
                personNode.SetAttribute("Отчество", person.Patronymic);

            parentNode.AppendChild(personNode);
        }

        /// <summary>
        /// Реквизиты документа, подтверждающего отгрузку товаров (работ, услуг, имущественных прав)
        /// </summary>
        /// <remarks>
        /// ДокПодтвОтгр
        /// </remarks>
        private static void AddTransferDocumentShipmentInfo(XmlDocument xmlDocument, XmlElement parentElement, SellerUniversalTransferDocument dataContract)
        {
            var documentShipmentList = dataContract.DocumentShipmentList;
            if (documentShipmentList == null || documentShipmentList.Count == 0)
                return;

            foreach (var documentShipment in documentShipmentList)
            {
                var documentShipmentInfoNode = xmlDocument.CreateElement("ДокПодтвОтгр"); // Реквизиты документа, подтверждающего отгрузку товаров (работ, услуг, имущественных прав) (графа 5а счёт-фактуры)

                if (!String.IsNullOrEmpty(documentShipment.Name))
                    documentShipmentInfoNode.SetAttribute("НаимДокОтгр", documentShipment.Name); // Наименование документа об отгрузке

                if (!String.IsNullOrEmpty(documentShipment.Number))
                    documentShipmentInfoNode.SetAttribute("НомДокОтгр", documentShipment.Number); // Номер документа об отгрузке

                if (documentShipment.Date != null)
                    documentShipmentInfoNode.SetAttribute("ДатаДокОтгр", documentShipment.Date.Value.ToString("dd.MM.yyyy")); // Дата документа об отгрузке

                parentElement.AppendChild(documentShipmentInfoNode);
            }
        }

        /// <summary>
        /// Сведения о лице, подписывающем файл обмена счета-фактуры (информации продавца) в электронной форме
        /// </summary>
        /// <remarks>
        /// Подписант
        /// </remarks>
        private static void AddSellerSigners(XmlDocument xmlDocument, XmlElement parentElement, SellerUniversalTransferDocument dataContract)
        {
            var signers = dataContract.Signers;
            if (signers == null || signers.Count == 0)
                throw new ArgumentNullException(nameof(dataContract.Signers));

            foreach (var signer in signers)
            {
                var signerNode = xmlDocument.CreateElement("Подписант"); // Сведения о лице, подписывающем файл обмена счета - фактуры(информации продавца) в электронной форме
                signerNode.SetAttribute("ОблПолн", ((int)signer.SignerAuthority).ToString()); // Область полномочий
                signerNode.SetAttribute("Статус", ((int)signer.SignerStatus).ToString());// Статус
                signerNode.SetAttribute("ОснПолн", signer.SignerAuthorityBase); // Основание полномочий (доверия)

                if (!String.IsNullOrEmpty(signer.SignerOrgAuthorityBase))
                    signerNode.SetAttribute("ОснПолнОрг", signer.SignerOrgAuthorityBase); // Основание полномочий (доверия) организации

                AddSellerSignerPersonInfo(xmlDocument, signer, signerNode);

                parentElement.AppendChild(signerNode);
            }
        }

        private static void AddSellerSignerPersonInfo(XmlDocument xmlDocument, SellerSigner signerInfo, XmlElement parentNode)
        {
            XmlElement signerPersonNode;
            if (signerInfo.LegalPersonRepresentative is LegalPersonRepresentative legalPersonRepresentative)
            {
                signerPersonNode = xmlDocument.CreateElement("ЮЛ"); // Представитель юридического лица

                if (!string.IsNullOrEmpty(legalPersonRepresentative.IndividualEntrepreneurRegistrationCertificate))
                    signerPersonNode.SetAttribute("ГосРегИПВыдДов", legalPersonRepresentative.IndividualEntrepreneurRegistrationCertificate);

                signerPersonNode.SetAttribute("ИННЮЛ", legalPersonRepresentative.Inn);

                if (!string.IsNullOrEmpty(legalPersonRepresentative.OrgName))
                    signerPersonNode.SetAttribute("НаимОрг", legalPersonRepresentative.OrgName);

                signerPersonNode.SetAttribute("Должн", legalPersonRepresentative.JobTitle); // Должность

                if (!string.IsNullOrEmpty(legalPersonRepresentative.OtherInfo))
                    signerPersonNode.SetAttribute("ИныеСвед", legalPersonRepresentative.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                AddPersonName(xmlDocument, legalPersonRepresentative, signerPersonNode);
            }
            else if (signerInfo.IndividualEntrepreneur is IndividualEntrepreneur individualEntrepreneur)
            {
                signerPersonNode = xmlDocument.CreateElement("ИП"); // Индивидуальный предприниматель

                if (individualEntrepreneur.IsHyphenInn)
                    signerPersonNode.SetAttribute("ДефИННФЛ", "-");
                else
                    signerPersonNode.SetAttribute("ИННФЛ", individualEntrepreneur.Inn);

                if (!string.IsNullOrEmpty(individualEntrepreneur.IndividualEntrepreneurRegistrationCertificate))
                    signerPersonNode.SetAttribute("СвГосРегИП", individualEntrepreneur.IndividualEntrepreneurRegistrationCertificate); // Реквизиты свидетельства о государственной регистрации индивидуального предпринимателя

                if (!string.IsNullOrEmpty(individualEntrepreneur.OtherInfo))
                    signerPersonNode.SetAttribute("ИныеСвед", individualEntrepreneur.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                AddPersonName(xmlDocument, individualEntrepreneur, signerPersonNode);
            }
            else if (signerInfo.PhysicalPerson is PhysicalPerson physicalPerson)
            {
                signerPersonNode = xmlDocument.CreateElement("ФЛ"); // Физическое лицо

                if (!string.IsNullOrEmpty(physicalPerson.Inn))
                    signerPersonNode.SetAttribute("ИННФЛ", physicalPerson.Inn);

                if (!string.IsNullOrEmpty(physicalPerson.IndividualEntrepreneurRegistrationCertificate))
                    signerPersonNode.SetAttribute("ГосРегИПВыдДов", physicalPerson.IndividualEntrepreneurRegistrationCertificate); // Реквизиты свидетельства о государственной регистрации индивидуального предпринимателя, выдавшего доверенность физическому лицу на подписание счета-фактуры

                if (!string.IsNullOrEmpty(physicalPerson.OtherInfo))
                    signerPersonNode.SetAttribute("ИныеСвед", physicalPerson.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                AddPersonName(xmlDocument, physicalPerson, signerPersonNode);
            }
            else
            {
                throw new ArgumentNullException("Не указана информация о подписанте.");
            }

            parentNode.AppendChild(signerPersonNode);
        }

        private static bool IsOrganizationsEquals(Organization org1, Organization org2)
        {
            if (org1.Okpo != org2.Okpo)
                return false;

            if (org1.OrganizationIdentificationInfo.LegalPerson is LegalPerson l1
                && org2.OrganizationIdentificationInfo.LegalPerson is LegalPerson l2)
                return l1.Inn == l2.Inn && l1.Kpp == l2.Kpp;

            if (org1.OrganizationIdentificationInfo.IndividualEntrepreneur is IndividualEntrepreneur i1
                && org2.OrganizationIdentificationInfo.IndividualEntrepreneur is IndividualEntrepreneur i2)
                return i1.Inn == i2.Inn;

            if (org1.OrganizationIdentificationInfo.PhysicalPerson is PhysicalPerson p1
                && org2.OrganizationIdentificationInfo.PhysicalPerson is PhysicalPerson p2)
                return p1.Inn == p2.Inn;

            return org1.ShortName == org2.ShortName;
        }

        private static string MakeXmlFormatted(XmlDocument xmlDocument)
        {
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xmlDocument.OuterXml);
                var formattedXml = (doc.Declaration != null ? doc.Declaration + Environment.NewLine : String.Empty) + doc.ToString();

                return formattedXml;
            }
            catch
            {
                return xmlDocument.OuterXml;
            }
        }
    }
}
