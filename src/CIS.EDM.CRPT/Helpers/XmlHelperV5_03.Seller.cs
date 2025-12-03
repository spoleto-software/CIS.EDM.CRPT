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
using CIS.EDM.Models.V5_03;
using CIS.EDM.Models.V5_03.Seller;
using CIS.EDM.Models.V5_03.Seller.Address;
using CIS.EDM.Models.V5_03.Seller.Reference;

namespace CIS.EDM.CRPT.Helpers
{
    internal static partial class XmlHelperV5_03
    {
        private const string FileEncoding = "windows-1251";

        private const string quantityToStringPattern = "0.######";

        public readonly static Encoding DefaultEncoding;

        static XmlHelperV5_03()
        {
            DefaultEncoding = Encoding.GetEncoding(FileEncoding);
        }

        public static DocumentData GenerateXml(CIS.EDM.Models.V5_03.Seller.SellerUniversalTransferDocument sellerDataContract)
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
            var xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", FileEncoding, null);
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
            var documentNode = xmlDocument.CreateElement("Документ");
            documentNode.SetAttribute("КНД", dataContract.TaxDocumentCode); // Классификатор налоговых документов
            if (!string.IsNullOrEmpty(dataContract.DocumentUid))
                documentNode.SetAttribute("УИД", dataContract.DocumentUid);

            documentNode.SetAttribute("Функция", dataContract.Function.ToString());

            var documentEconomicName = !string.IsNullOrEmpty(dataContract.DocumentEconomicName) ? dataContract.DocumentEconomicName : dataContract.Function.GetDocumentEconomicName();
            if (documentEconomicName != null)
                documentNode.SetAttribute("ПоФактХЖ", documentEconomicName); // Наименование документа по факту хозяйственной жизни

            var documentName = !string.IsNullOrEmpty(dataContract.DocumentName) ? dataContract.DocumentName : dataContract.Function.GetDocumentName();
            if (documentName != null)
                documentNode.SetAttribute("НаимДокОпр", documentName); // Наименование первичного документа, определенное организацией (согласованное сторонами сделки)

            documentNode.SetAttribute("ДатаИнфПр", dataContract.DateCreation.ToString("dd.MM.yyyy")); // Дата формирования файла обмена счета-фактуры (информации продавца)
            documentNode.SetAttribute("ВремИнфПр", dataContract.DateCreation.ToString("HH.mm.ss")); // Время формирования файла обмена счета-фактуры (информации продавца)
            documentNode.SetAttribute("НаимЭконСубСост", GetEconomicEntityName(dataContract.DocumentCreator)); // Наименование экономического субъекта – составителя файла обмена счета-фактуры (информации продавца)

            if (!string.IsNullOrEmpty(dataContract.ApprovedStructureAdditionalInfoFields))
                documentNode.SetAttribute("СоглСтрДопИнф", dataContract.ApprovedStructureAdditionalInfoFields); // Информация о наличии согласованной структуры дополнительных информационных полей

            xmlDocument.DocumentElement.AppendChild(documentNode);

            // СвСчФакт
            AddInvoiceDocumentInfo(xmlDocument, dataContract, documentNode);

            // ТаблСчФакт
            AddObjectiveItems(xmlDocument, documentNode, dataContract);

            // СвПродПер
            AddTransferInfo(xmlDocument, documentNode, dataContract);

            // Подписант
            AddSellerSigners(xmlDocument, documentNode, dataContract);

            if (dataContract.DocumentCreatorBase is EDM.Models.V5_03.Document creatorBase)
                AddDocumentInfo(xmlDocument, documentNode, [creatorBase], "ОснДоверОргСост");// Основание, по которому экономический субъект является составителем файла обмена счета-фактуры (информации продавца)
        }

        /// <summary>
        /// Сведения о счете-фактуре (содержание факта хозяйственной жизни 1 - сведения об участниках факта хозяйственной жизни, основаниях и обстоятельствах его проведения)
        /// </summary>
        /// <remarks>
        /// СвСчФакт
        /// </remarks>
        private static void AddInvoiceDocumentInfo(XmlDocument xmlDocument, SellerUniversalTransferDocument dataContract, XmlElement documentNode)
        {
            var documentInfoNode = xmlDocument.CreateElement("СвСчФакт");
            documentInfoNode.SetAttribute("НомерДок", dataContract.DocumentNumber);
            documentInfoNode.SetAttribute("ДатаДок", dataContract.DocumentDate.ToString("dd.MM.yyyy"));
            if (!string.IsNullOrEmpty(dataContract.CorrectedSellerFileName))
            {
                documentInfoNode.SetAttribute("ИмяФайлИспрПрод", dataContract.CorrectedSellerFileName);
            }

            if (!string.IsNullOrEmpty(dataContract.CorrectedBuyerFileName))
            {
                documentInfoNode.SetAttribute("ИмяФайлИспрПок", dataContract.CorrectedBuyerFileName);
            }

            AddRevisionInfo(xmlDocument, documentInfoNode, dataContract);

            documentNode.AppendChild(documentInfoNode);

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

            // СвПРД
            AddPaymentDocumentInfo(xmlDocument, documentInfoNode, dataContract.PaymentDocumentInfoList);

            // ДокПодтвОтгрНом
            AddDocumentInfo(xmlDocument, documentInfoNode, dataContract.DocumentShipmentList, "ДокПодтвОтгрНом");

            // Сведения о покупателе (строки 6, 6а, 6б счета-фактуры)
            foreach (var buyer in dataContract.Buyers)
                AddFirmInformation(xmlDocument, documentInfoNode, buyer, "СвПокуп");

            if (!string.IsNullOrEmpty(dataContract.CurrencyCode))
            {
                var currencyrNode = xmlDocument.CreateElement("ДенИзм");
                currencyrNode.SetAttribute("КодОКВ", dataContract.CurrencyCode); //Валюта: Код

                if (!string.IsNullOrEmpty(dataContract.CurrencyName))
                    currencyrNode.SetAttribute("НаимОКВ", dataContract.CurrencyName);

                if (dataContract.CurrencyRate > 0M)
                    currencyrNode.SetAttribute("КурсВал", dataContract.CurrencyRate.Value.ToString("0.00##", CultureInfo.InvariantCulture));

                documentInfoNode.AppendChild(currencyrNode);
            }

            // ДопСвФХЖ1
            AddAdditionalInfo(xmlDocument, documentInfoNode, dataContract);

            // Информационное поле факта хозяйственной жизни 1
            AddOtherEconomicInfo(xmlDocument, documentInfoNode, dataContract.OtherEconomicInfo, "ИнфПолФХЖ1");
        }

        /// <summary>
        /// Исправление (строка 1а счета - фактуры)
        /// </summary>
        private static void AddRevisionInfo(XmlDocument xmlDocument, XmlElement parentElement, SellerUniversalTransferDocument dataContract)
        {
            if (!string.IsNullOrEmpty(dataContract.RevisionNumber))
            {
                var revisionNode = xmlDocument.CreateElement("ИспрДок"); // Исправление (строка 1а счета - фактуры)

                revisionNode.SetAttribute("НомИспр", dataContract.RevisionNumber);
                revisionNode.SetAttribute("ДатаИспр", dataContract.RevisionDate.Value.ToString("dd.MM.yyyy"));

                parentElement.AppendChild(revisionNode);
            }
        }

        /// <summary>
        /// Дополнительные сведения об участниках факта хозяйственной жизни, основаниях и обстоятельствах его проведения
        /// </summary>
        /// <remarks>
        /// ДопСвФХЖ1
        /// </remarks>
        private static void AddAdditionalInfo(XmlDocument xmlDocument, XmlElement parentElement, SellerUniversalTransferDocument dataContract)
        {
            var additionalTransactionParticipantInfo = dataContract.AdditionalTransactionParticipantInfo;

            if (additionalTransactionParticipantInfo == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(additionalTransactionParticipantInfo.GovernmentContractInfo)
                || additionalTransactionParticipantInfo.InvoiceFormationType != InvoiceFormationType.NotSpecified
                || !string.IsNullOrEmpty(additionalTransactionParticipantInfo.UPDInvoiceFormationType)
                || !string.IsNullOrEmpty(additionalTransactionParticipantInfo.UPDFormationType)
                || additionalTransactionParticipantInfo.ObligationInfoList != null
                || additionalTransactionParticipantInfo.SellerInfoCircumPublicProc != null
                || additionalTransactionParticipantInfo.FactorInfo != null
                || additionalTransactionParticipantInfo.MainAssignMonetaryClaim != null
                || additionalTransactionParticipantInfo.SupportingDocumentList != null)
            {
                var economicNode = xmlDocument.CreateElement("ДопСвФХЖ1");

                if (!string.IsNullOrEmpty(additionalTransactionParticipantInfo.GovernmentContractInfo))
                    economicNode.SetAttribute("ИдГосКон", additionalTransactionParticipantInfo.GovernmentContractInfo);

                if (dataContract.Function == UniversalTransferDocumentFunction.СЧФ
                    && additionalTransactionParticipantInfo.InvoiceFormationType != InvoiceFormationType.NotSpecified)
                    economicNode.SetAttribute("СпОбстФСЧФ", ((int)additionalTransactionParticipantInfo.InvoiceFormationType).ToString());

                if (dataContract.Function == UniversalTransferDocumentFunction.СЧФДОП
                    && !string.IsNullOrEmpty(additionalTransactionParticipantInfo.UPDInvoiceFormationType))
                    economicNode.SetAttribute("СпОбстФСЧФДОП", additionalTransactionParticipantInfo.UPDInvoiceFormationType);

                if (dataContract.Function == UniversalTransferDocumentFunction.ДОП
                    && !string.IsNullOrEmpty(additionalTransactionParticipantInfo.UPDFormationType))
                    economicNode.SetAttribute("СпОбстФДОП", additionalTransactionParticipantInfo.UPDFormationType);

                if (additionalTransactionParticipantInfo.ObligationInfoList != null)
                {
                    foreach (var obligationInfo in additionalTransactionParticipantInfo.ObligationInfoList)
                    {
                        var obligationTypeNode = xmlDocument.CreateElement("ВидОбяз");

                        if (!string.IsNullOrEmpty(obligationInfo.Code))
                            obligationTypeNode.SetAttribute("КодВидОбяз", obligationInfo.Code);

                        if (!string.IsNullOrEmpty(obligationInfo.Name))
                            obligationTypeNode.SetAttribute("НаимВидОбяз", obligationInfo.Name);

                        economicNode.AppendChild(obligationTypeNode);
                    }
                }

                if (additionalTransactionParticipantInfo.SellerInfoCircumPublicProc is SellerInfoCircumPublicProc sellerInfoCircumPublicProc)
                {
                    var circumPublicProcNode = xmlDocument.CreateElement("ИнфПродЗаГосКазн");
                    circumPublicProcNode.SetAttribute("ДатаГосКонт", sellerInfoCircumPublicProc.DateStateContract.ToString("dd.MM.yyyy"));
                    circumPublicProcNode.SetAttribute("НомерГосКонт", sellerInfoCircumPublicProc.NumberStateContract);

                    if (!string.IsNullOrEmpty(sellerInfoCircumPublicProc.PersonalAccountSeller))
                        circumPublicProcNode.SetAttribute("ЛицСчетПрод", sellerInfoCircumPublicProc.PersonalAccountSeller);

                    if (!string.IsNullOrEmpty(sellerInfoCircumPublicProc.SellerBudgetClassCode))
                        circumPublicProcNode.SetAttribute("КодПродБюджКласс", sellerInfoCircumPublicProc.SellerBudgetClassCode);

                    if (!string.IsNullOrEmpty(sellerInfoCircumPublicProc.SellerTargetCode))
                        circumPublicProcNode.SetAttribute("КодЦелиПрод", sellerInfoCircumPublicProc.SellerTargetCode);

                    if (!string.IsNullOrEmpty(sellerInfoCircumPublicProc.SellerTreasuryCode))
                        circumPublicProcNode.SetAttribute("КодКазначПрод", sellerInfoCircumPublicProc.SellerTreasuryCode);

                    if (!string.IsNullOrEmpty(sellerInfoCircumPublicProc.SellerTreasuryName))
                        circumPublicProcNode.SetAttribute("НаимКазначПрод", sellerInfoCircumPublicProc.SellerTreasuryName);

                    economicNode.AppendChild(circumPublicProcNode);
                }

                if (additionalTransactionParticipantInfo.FactorInfo is Organization factor)
                {
                    AddFirmInformation(xmlDocument, economicNode, factor, "СвФактор");
                }

                if (additionalTransactionParticipantInfo.MainAssignMonetaryClaim is EDM.Models.V5_03.Document monetaryClaim)
                {
                    AddDocumentInfo(xmlDocument, economicNode, [monetaryClaim], "ОснУстДенТреб");
                }

                AddDocumentInfo(xmlDocument, economicNode, additionalTransactionParticipantInfo.SupportingDocumentList, "СопрДокФХЖ");

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
                if (!string.IsNullOrEmpty(paymentDocumentInfo.Number))
                    paymentDocumentInfoNode.SetAttribute("НомерПРД", paymentDocumentInfo.Number); // Номер платежно-расчетного документа

                if (paymentDocumentInfo.Date != null)
                    paymentDocumentInfoNode.SetAttribute("ДатаПРД", paymentDocumentInfo.Date.Value.ToString("dd.MM.yyyy")); // Дата составления платежно-расчетного документа

                if (paymentDocumentInfo.Total != null)
                    paymentDocumentInfoNode.SetAttribute("СуммаПРД", paymentDocumentInfo.Total.Value.ToString("0.00", CultureInfo.InvariantCulture)); // Сумма

                parentElement.AppendChild(paymentDocumentInfoNode);
            }
        }

        private static string GetEconomicEntityName(Organization firm)
        {
            if (firm.OrganizationIdentificationInfo.LegalPerson is LegalPerson legalPerson)
            {
                if (!string.IsNullOrEmpty(legalPerson.Inn)
                    && !string.IsNullOrEmpty(legalPerson.Kpp))
                {
                    return $"{legalPerson.Name}, ИНН/КПП: {legalPerson.Inn}/{legalPerson.Kpp}";
                }
                else if (!string.IsNullOrEmpty(legalPerson.Inn))
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
                if (!string.IsNullOrEmpty(individualEntrepreneur.Inn))
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
                if (!string.IsNullOrEmpty(physicalPerson.Inn))
                {
                    return $"{physicalPerson.GetFullName()}, ИНН: {physicalPerson.Inn}";
                }
                else
                {
                    return physicalPerson.GetFullName();
                }
            }
            else if (firm.OrganizationIdentificationInfo.ForeignPerson is ForeignEntity foreignPerson)
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
            if (!string.IsNullOrEmpty(firm.Okpo))
                firmNode.SetAttribute("ОКПО", firm.Okpo);

            if (!string.IsNullOrEmpty(firm.OpfCode))
                firmNode.SetAttribute("КодОПФ", firm.OpfCode);

            if (!string.IsNullOrEmpty(firm.OpfName))
                firmNode.SetAttribute("ПолнНаимОПФ", firm.OpfName);

            if (!string.IsNullOrEmpty(firm.Department))
                firmNode.SetAttribute("СтруктПодр", firm.Department); // Структурное подразделение

            if (!string.IsNullOrEmpty(firm.OrganizationAdditionalInfo))
                firmNode.SetAttribute("ИнфДляУчаст", firm.OrganizationAdditionalInfo); // Информация для участника документооборота

            if (!string.IsNullOrEmpty(firm.ShortName))
                firmNode.SetAttribute("СокрНаим", firm.ShortName);

            var firmIdNode = xmlDocument.CreateElement("ИдСв"); // Идентификационные сведения

            if (firm.OrganizationIdentificationInfo.IndividualEntrepreneur is IndividualEntrepreneur individualEntrepreneur)
            {
                var firmInfoNode = xmlDocument.CreateElement("СвИП"); // Сведения об индивидуальном предпринимателе

                firmInfoNode.SetAttribute("ИННФЛ", individualEntrepreneur.Inn);

                if (!string.IsNullOrEmpty(individualEntrepreneur.IndividualEntrepreneurRegistrationCertificate))
                    firmInfoNode.SetAttribute("СвГосРегИП", individualEntrepreneur.IndividualEntrepreneurRegistrationCertificate); // Реквизиты свидетельства о государственной регистрации индивидуального предпринимателя

                if (!string.IsNullOrEmpty(individualEntrepreneur.Ogrnip))
                    firmInfoNode.SetAttribute("ОГРНИП", individualEntrepreneur.Ogrnip); // Основной государственный регистрационный номер индивидуального предпринимателя

                if (individualEntrepreneur.OgrnipDate != null)
                    firmInfoNode.SetAttribute("ДатаОГРНИП", individualEntrepreneur.OgrnipDate.Value.ToString("dd.MM.yyyy")); // Дата присвоения основного государственного регистрационного номера индивидуального предпринимателя

                if (!string.IsNullOrEmpty(individualEntrepreneur.OtherInfo))
                    firmInfoNode.SetAttribute("ИныеСвед", individualEntrepreneur.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                AddPersonName(xmlDocument, individualEntrepreneur, firmInfoNode);

                firmIdNode.AppendChild(firmInfoNode);
                firmNode.AppendChild(firmIdNode);
            }
            else if (firm.OrganizationIdentificationInfo.LegalPerson is LegalPerson legalPerson)
            {
                var firmInfoNode = xmlDocument.CreateElement("СвЮЛУч"); // Сведения о юридическом лице, состоящем на учете в налоговых органах
                firmInfoNode.SetAttribute("НаимОрг", legalPerson.Name);
                firmInfoNode.SetAttribute("ИННЮЛ", legalPerson.Inn);

                if (!string.IsNullOrEmpty(legalPerson.Kpp))
                    firmInfoNode.SetAttribute("КПП", legalPerson.Kpp);

                firmIdNode.AppendChild(firmInfoNode);

                firmNode.AppendChild(firmIdNode);
            }
            else if (firm.OrganizationIdentificationInfo.PhysicalPerson is PhysicalPerson physicalPerson)
            {
                var firmInfoNode = xmlDocument.CreateElement("СвФЛУч"); // Сведения о физическом лице
                if (!string.IsNullOrEmpty(physicalPerson.Inn))
                    firmInfoNode.SetAttribute("ИННФЛ", physicalPerson.Inn);

                if (!string.IsNullOrEmpty(physicalPerson.Status))
                    firmInfoNode.SetAttribute("ИдСтатЛ", physicalPerson.Status); // Идентификация статуса лица

                if (!string.IsNullOrEmpty(physicalPerson.OtherInfo))
                    firmInfoNode.SetAttribute("ИныеСвед", physicalPerson.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                AddPersonName(xmlDocument, physicalPerson, firmInfoNode);

                firmIdNode.AppendChild(firmInfoNode);

                firmNode.AppendChild(firmIdNode);
            }
            else if (firm.OrganizationIdentificationInfo.ForeignPerson is ForeignEntity foreignPerson)
            {
                AddForeignEntity(xmlDocument, firmIdNode, foreignPerson, "СвИнНеУч");

                firmNode.AppendChild(firmIdNode);
            }
            else
            {
                throw new ArgumentNullException("Не указаны идентификационные сведения организации.");
            }

            AddAddressInformation(xmlDocument, firm.Address, firmNode);

            AddBankDetailsInformation(xmlDocument, firm.BankAccountDetails, firmNode);

            AddContactInformation(xmlDocument, firm.Contact, firmNode);

            parentElement.AppendChild(firmNode);
        }

        private static void AddAddressInformation(XmlDocument xmlDocument, Address address, XmlElement parentElement)
        {
            if (address.RussianAddress is RussianAddress russianAddress)
            {
                var russianAddressNode = xmlDocument.CreateElement("Адрес");
                if (!string.IsNullOrEmpty(address.GlobalLocationNumber))
                {
                    russianAddressNode.SetAttribute("ГЛНМеста", address.GlobalLocationNumber);
                }

                var russianAddressInfoNode = xmlDocument.CreateElement("АдрРФ");
                if (!string.IsNullOrEmpty(russianAddress.ZipCode))
                    russianAddressInfoNode.SetAttribute("Индекс", russianAddress.ZipCode);

                russianAddressInfoNode.SetAttribute("КодРегион", russianAddress.RegionCode);
                russianAddressInfoNode.SetAttribute("НаимРегион", russianAddress.Region);

                if (!string.IsNullOrEmpty(russianAddress.Territory))
                    russianAddressInfoNode.SetAttribute("Район", russianAddress.Territory);

                if (!string.IsNullOrEmpty(russianAddress.City))
                    russianAddressInfoNode.SetAttribute("Город", russianAddress.City);

                if (!string.IsNullOrEmpty(russianAddress.Locality))
                    russianAddressInfoNode.SetAttribute("НаселПункт", russianAddress.Locality);

                if (!string.IsNullOrEmpty(russianAddress.Street))
                    russianAddressInfoNode.SetAttribute("Улица", russianAddress.Street);

                if (!string.IsNullOrEmpty(russianAddress.Building))
                    russianAddressInfoNode.SetAttribute("Дом", russianAddress.Building);

                if (!string.IsNullOrEmpty(russianAddress.Block))
                    russianAddressInfoNode.SetAttribute("Корпус", russianAddress.Block);

                if (!string.IsNullOrEmpty(russianAddress.Apartment))
                    russianAddressInfoNode.SetAttribute("Кварт", russianAddress.Apartment);

                if (!string.IsNullOrEmpty(russianAddress.OtherInfo))
                    russianAddressInfoNode.SetAttribute("ИныеСвед", russianAddress.OtherInfo);

                russianAddressNode.AppendChild(russianAddressInfoNode);
                parentElement.AppendChild(russianAddressNode);
            }
            else if (address.AddressCode is AddressCode addressCode)
            {
                var addressCodeNode = xmlDocument.CreateElement("Адрес");
                if (!string.IsNullOrEmpty(address.GlobalLocationNumber))
                {
                    addressCodeNode.SetAttribute("ГЛНМеста", address.GlobalLocationNumber);
                }

                var addressCodeInfoNode = xmlDocument.CreateElement("АдрГАР");
                addressCodeInfoNode.SetAttribute("ИдНом", addressCode.UniqueCode.ToString());
                
                if (!string.IsNullOrEmpty(addressCode.ZipCode))
                    addressCodeInfoNode.SetAttribute("Индекс", addressCode.ZipCode);

                if (!string.IsNullOrEmpty(addressCode.Region))
                {
                    var regionAddressInfoNode = xmlDocument.CreateElement("Регион");
                    regionAddressInfoNode.InnerText = addressCode.Region;

                    addressCodeInfoNode.AppendChild(regionAddressInfoNode);
                }

                if (!string.IsNullOrEmpty(addressCode.RegionName))
                {
                    var regionNameAddressInfoNode = xmlDocument.CreateElement("НаимРегион");
                    regionNameAddressInfoNode.InnerText = addressCode.RegionName;

                    addressCodeInfoNode.AppendChild(regionNameAddressInfoNode);
                }

                if (addressCode.MunicipalDistrict is AddressElementCode municipal)
                {
                    var addressInfoNode = xmlDocument.CreateElement("МуниципРайон");
                    addressInfoNode.SetAttribute("ВидКод", municipal.TypeCode);
                    addressInfoNode.SetAttribute("Наим", municipal.Name);

                    addressCodeInfoNode.AppendChild(addressInfoNode);
                }

                if (addressCode.UrbanRuralSettlement is AddressElementCode urban)
                {
                    var addressInfoNode = xmlDocument.CreateElement("ГородСелПоселен");
                    addressInfoNode.SetAttribute("ВидКод", urban.TypeCode);
                    addressInfoNode.SetAttribute("Наим", urban.Name);

                    addressCodeInfoNode.AppendChild(addressInfoNode);
                }

                if (addressCode.Settlement is AddressElement settlement)
                {
                    var addressInfoNode = xmlDocument.CreateElement("НаселенПункт");
                    addressInfoNode.SetAttribute("Вид", settlement.Type);
                    addressInfoNode.SetAttribute("Наим", settlement.Name);

                    addressCodeInfoNode.AppendChild(addressInfoNode);
                }

                if (addressCode.PlanningStructureElement is AddressElementType planningStructure)
                {
                    var addressInfoNode = xmlDocument.CreateElement("ЭлПланСтруктур");
                    addressInfoNode.SetAttribute("Тип", planningStructure.Type);
                    addressInfoNode.SetAttribute("Наим", planningStructure.Name);

                    addressCodeInfoNode.AppendChild(addressInfoNode);
                }

                if (addressCode.StreetRoadNetworkElement is AddressElementType streetRoadNetwork)
                {
                    var addressInfoNode = xmlDocument.CreateElement("ЭлУлДорСети");
                    addressInfoNode.SetAttribute("Тип", streetRoadNetwork.Type);
                    addressInfoNode.SetAttribute("Наим", streetRoadNetwork.Name);

                    addressCodeInfoNode.AppendChild(addressInfoNode);
                }

                if (!string.IsNullOrEmpty(addressCode.LandPlotNumber))
                {
                    var addressInfoNode = xmlDocument.CreateElement("ЗемелУчасток");
                    addressInfoNode.InnerText = addressCode.LandPlotNumber;

                    addressCodeInfoNode.AppendChild(addressInfoNode);
                }

                if (addressCode.BuildingList != null)
                {
                    foreach (var building in addressCode.BuildingList)
                    {
                        var addressInfoNode = xmlDocument.CreateElement("Здание");
                        addressInfoNode.SetAttribute("Тип", building.Type);
                        addressInfoNode.SetAttribute("Номер", building.Number);

                        addressCodeInfoNode.AppendChild(addressInfoNode);
                    }
                }

                if (addressCode.BuildingRoom is AddressElementNumber buildingRoom)
                {
                    var addressInfoNode = xmlDocument.CreateElement("ПомещЗдания");
                    addressInfoNode.SetAttribute("Тип", buildingRoom.Type);
                    addressInfoNode.SetAttribute("Номер", buildingRoom.Number);

                    addressCodeInfoNode.AppendChild(addressInfoNode);
                }

                if (addressCode.ApartmentRoom is AddressElementNumber apartmentRoom)
                {
                    var addressInfoNode = xmlDocument.CreateElement("ПомещКвартиры");
                    addressInfoNode.SetAttribute("Тип", apartmentRoom.Type);
                    addressInfoNode.SetAttribute("Номер", apartmentRoom.Number);

                    addressCodeInfoNode.AppendChild(addressInfoNode);
                }

                addressCodeNode.AppendChild(addressCodeInfoNode);
                parentElement.AppendChild(addressCodeNode);
            }
            else if (address.AddressInformation is AddressInformation addressInformation)
            {
                var addressInfoNode = xmlDocument.CreateElement("Адрес");
                if (!string.IsNullOrEmpty(address.GlobalLocationNumber))
                {
                    addressInfoNode.SetAttribute("ГЛНМеста", address.GlobalLocationNumber);
                }

                var infoAddressInfoNode = xmlDocument.CreateElement("АдрИнф");

                infoAddressInfoNode.SetAttribute("КодСтр", addressInformation.CountryCode);
                infoAddressInfoNode.SetAttribute("НаимСтран", addressInformation.CountryName);
                infoAddressInfoNode.SetAttribute("АдрТекст", addressInformation.Address);

                addressInfoNode.AppendChild(infoAddressInfoNode);
                parentElement.AppendChild(addressInfoNode);
            }
        }

        private static void AddContactInformation(XmlDocument xmlDocument, Contact contact, XmlElement firmNode)
        {
            if (contact != null)
            {
                var contactNode = xmlDocument.CreateElement("Контакт"); // Контактные данные
                if (!string.IsNullOrEmpty(contact.OtherInfo))
                {
                    contactNode.SetAttribute("ИнКонт", contact.OtherInfo);
                }

                if (contact.PhoneList != null)
                {
                    foreach (var phone in contact.PhoneList)
                    {
                        if (!string.IsNullOrEmpty(phone))
                        {
                            var phoneNode = xmlDocument.CreateElement("Тлф");

                            phoneNode.InnerText = phone;// Номер контактного телефона/факс

                            contactNode.AppendChild(phoneNode);
                        }
                    }
                }

                if (contact.EmailList != null)
                {
                    foreach (var email in contact.EmailList)
                    {
                        if (!string.IsNullOrEmpty(email))
                        {
                            var emailNode = xmlDocument.CreateElement("ЭлПочта");

                            emailNode.InnerText = email;// Адрес электронной почты

                            contactNode.AppendChild(emailNode);
                        }
                    }
                }

                firmNode.AppendChild(contactNode);
            }
        }

        private static void AddBankDetailsInformation(XmlDocument xmlDocument, BankAccountDetails bankAccountDetails, XmlElement parentElement)
        {
            if (bankAccountDetails != null)
            {
                var bankNode = xmlDocument.CreateElement("БанкРекв"); // Банковские реквизиты
                if (!string.IsNullOrEmpty(bankAccountDetails.BankAccountNumber))
                    bankNode.SetAttribute("НомерСчета", bankAccountDetails.BankAccountNumber); // Номер банковского счета

                var bankDetails = bankAccountDetails.BankDetails;
                if (bankDetails != null)
                {
                    var bankInfoNode = xmlDocument.CreateElement("СвБанк"); // Сведения о банке
                    if (!string.IsNullOrEmpty(bankDetails.BankName))
                        bankInfoNode.SetAttribute("НаимБанк", bankDetails.BankName); // Наименование банка

                    if (!string.IsNullOrEmpty(bankDetails.BankId))
                        bankInfoNode.SetAttribute("БИК", bankDetails.BankId); // БИК

                    if (!string.IsNullOrEmpty(bankDetails.CorrespondentAccount))
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
                if (!string.IsNullOrEmpty(item.Id))
                    additionalInfoNode.SetAttribute("Идентиф", item.Id); // Идентификатор

                if (!string.IsNullOrEmpty(item.Value))
                    additionalInfoNode.SetAttribute("Значен", item.Value); // Значение

                parentElement.AppendChild(additionalInfoNode);
            }
        }

        /// <summary>
        /// Информационное поле факта хозяйственной жизни (1, 3, 4)
        /// </summary>
        private static void AddOtherEconomicInfo(XmlDocument xmlDocument, XmlElement parentElement, OtherEconomicInfo otherEconomicInfo, string elementName)
        {
            if (otherEconomicInfo == null)
            {
                return;
            }

            var additionalInfoNode = xmlDocument.CreateElement(elementName); // Информационное поле факта хозяйственной жизни
            if (!string.IsNullOrEmpty(otherEconomicInfo.InfoFileId))
                additionalInfoNode.SetAttribute("ИдФайлИнфПол", otherEconomicInfo.InfoFileId); // Идентификатор файла информационного поля

            if (otherEconomicInfo.Items != null)
            {
                foreach (var item in otherEconomicInfo.Items)
                {
                    var additionalInfoTextNode = xmlDocument.CreateElement("ТекстИнф"); // Текстовая информация
                    if (!string.IsNullOrEmpty(item.Id))
                        additionalInfoTextNode.SetAttribute("Идентиф", item.Id); // Идентификатор

                    if (!string.IsNullOrEmpty(item.Value))
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

                if (!string.IsNullOrEmpty(item.UnitCode))
                    itemNode.SetAttribute("ОКЕИ_Тов", item.UnitCode); // Код единицы измерения (графа 2 счета-фактуры)

                if (!string.IsNullOrEmpty(item.UnitName))
                    itemNode.SetAttribute("НаимЕдИзм", item.UnitName); // Наименование единицы измерения (условное обозначение национальное, графа 2а счета-фактуры)

                if (item.Quantity > 0)
                    itemNode.SetAttribute("КолТов", item.Quantity.ToString(quantityToStringPattern, CultureInfo.InvariantCulture)); // Количество (объем) (графа 3 счета - фактуры)

                if (item.Price > 0)
                    itemNode.SetAttribute("ЦенаТов", item.Price.ToString("0.00", CultureInfo.InvariantCulture)); // Цена (тариф) за единицу измерения (графа 4 счета-фактуры)

                if (item.SumWithoutVat > 0)
                    itemNode.SetAttribute("СтТовБезНДС", item.SumWithoutVat.ToString("0.00", CultureInfo.InvariantCulture)); // Стоимость товаров (работ, услуг), имущественных прав без налога - всего (графа 5 счета-фактуры)

                itemNode.SetAttribute("НалСт", item.TaxRate.GetTaxRateName());// Налоговая ставка (графа 7 счета-фактуры)

                itemNode.SetAttribute("СтТовУчНал", item.Sum.Value.ToString("0.00", CultureInfo.InvariantCulture)); // Стоимость товаров(работ, услуг), имущественных прав с налогом - всего (графа 9 счетафактуры)

                // СвДТ
                AddCustomDeclarationsInformation(xmlDocument, item, itemNode);

                // ДопСведТов
                AddAdditionalInfo(xmlDocument, item, itemNode);

                // Акциз
                AddExciseValue(xmlDocument, item, itemNode);

                // СумНал
                var taxNode = xmlDocument.CreateElement("СумНал");// Сумма налога, предъявляемая покупателю (графа 8 счета-фактуры)
                AddVatValue(xmlDocument, item.TaxRate == TaxRate.WithoutVat, item.Vat, taxNode);

                itemNode.AppendChild(taxNode);

                // ИнфПолФХЖ2
                AddOtherEconomicInfo(xmlDocument, itemNode, item.OtherEconomicInfoItemList, "ИнфПолФХЖ2");

                objectiveNode.AppendChild(itemNode);
            }

            // ВсегоОпл
            AddObjectiveTotal(xmlDocument, dataContract.Items, objectiveNode);

            parentElement.AppendChild(objectiveNode);
        }

        /// <summary>
        /// Реквизиты строки «Всего к оплате» (строка 9 счета-фактуры).
        /// </summary>
        private static void AddObjectiveTotal(XmlDocument xmlDocument, List<InvoiceItem> items, XmlElement objectiveNode)
        {
            var totalPaidNode = xmlDocument.CreateElement("ВсегоОпл"); // Реквизиты строки «Всего к оплате»
            var total = items.Sum(x => x.SumWithoutVat);
            if (total > 0)
                totalPaidNode.SetAttribute("СтТовБезНДСВсего", total.ToString("0.00", CultureInfo.InvariantCulture)); // Всего к оплате, Стоимость товаров (работ, услуг), имущественных прав без налога - всего(строка «Всего к оплате»/ графа 5 счета - фактуры)

            totalPaidNode.SetAttribute("СтТовУчНалВсего", items.Sum(x => (x.Sum ?? 0M)).ToString("0.00", CultureInfo.InvariantCulture)); // Всего к оплате, Стоимость товаров(работ, услуг), имущественных прав с налогом - всего (строка «Всего к оплате»/ графа 9 счета - фактуры)

            var totalQuantity = items.Sum(x => x.Quantity);
            if (totalQuantity > 0)
                totalPaidNode.SetAttribute("КолНеттоВс", totalQuantity.ToString(quantityToStringPattern, CultureInfo.InvariantCulture));

            var totalTaxNode = xmlDocument.CreateElement("СумНалВсего"); // Всего к оплате, Сумма налога, предъявляемая покупателю (строка «Всего к оплате»/ графа 8 счета-фактуры)
            var isWithoutVat = !items.Any(x => x.TaxRate != TaxRate.WithoutVat);
            AddVatValue(xmlDocument, isWithoutVat, items.Sum(x => x.Vat), totalTaxNode);
            totalPaidNode.AppendChild(totalTaxNode);

            objectiveNode.AppendChild(totalPaidNode);
        }

        /// <summary>
        /// ДопСведТов
        /// </summary>
        private static void AddAdditionalInfo(XmlDocument xmlDocument, InvoiceItem item, XmlElement itemNode)
        {
            var additionalInfo = item.AdditionalInfo;
            if (additionalInfo == null)
            {
                return;
            }

            var additionalInfoNode = xmlDocument.CreateElement("ДопСведТов"); // Дополнительные сведения об отгруженных товарах (выполненных работах, оказанных услугах), переданных имущественных правах

            if (additionalInfo.Type != InvoiceItemType.NotSpecified)
                additionalInfoNode.SetAttribute("ПрТовРаб", ((int)additionalInfo.Type).ToString()); // Признак Товар/Работа/Услуга/Право/Иное

            if (!string.IsNullOrEmpty(additionalInfo.AdditionalTypeInfo))
                additionalInfoNode.SetAttribute("ДопПризн", additionalInfo.AdditionalTypeInfo); // Дополнительная информация о признаке

            if (additionalInfo.ItemToRelease != null)
                additionalInfoNode.SetAttribute("НадлОтп", additionalInfo.ItemToRelease.Value.ToString("0.#######", CultureInfo.InvariantCulture)); // Заказанное количество (количество надлежит отпустить)

            if (!string.IsNullOrEmpty(additionalInfo.Characteristic))
                additionalInfoNode.SetAttribute("ХарактерТов", additionalInfo.Characteristic); // Характеристика/описание товара (в том числе графа 1 счета-фактуры) 

            if (!string.IsNullOrEmpty(additionalInfo.Kind))
                additionalInfoNode.SetAttribute("СортТов", additionalInfo.Kind); // Сорт товара

            if (!string.IsNullOrEmpty(additionalInfo.Series))
                additionalInfoNode.SetAttribute("СерияТов", additionalInfo.Series); // Серия товара

            if (!string.IsNullOrEmpty(additionalInfo.Article))
                additionalInfoNode.SetAttribute("АртикулТов", additionalInfo.Article);// Артикул товара (в том числе графа 1 счета-фактуры)

            if (!string.IsNullOrEmpty(additionalInfo.Code))
                additionalInfoNode.SetAttribute("КодТов", additionalInfo.Code); // Код товара (в том числе графа 1 счета-фактуры)

            if (!string.IsNullOrEmpty(additionalInfo.GlobalTradeItemNumber))
                additionalInfoNode.SetAttribute("ГТИН", additionalInfo.GlobalTradeItemNumber); // Глобальный идентификационный номер товарной продукции

            if (!string.IsNullOrEmpty(additionalInfo.CatalogCode))
                additionalInfoNode.SetAttribute("КодКат", additionalInfo.CatalogCode); // Код каталога

            if (!string.IsNullOrEmpty(additionalInfo.FeaccCode))
                additionalInfoNode.SetAttribute("КодВидТов", additionalInfo.FeaccCode); // Код вида товара (графа 1б счета-фактуры)

            if (!string.IsNullOrEmpty(additionalInfo.ProductTypeCode))
                additionalInfoNode.SetAttribute("КодВидПр", additionalInfo.ProductTypeCode);

            if (!string.IsNullOrEmpty(additionalInfo.Okdp2ProductCode))
                additionalInfoNode.SetAttribute("КодТовОКДП2", additionalInfo.Okdp2ProductCode);

            if (!string.IsNullOrEmpty(additionalInfo.AdditionalOperationInfo))
                additionalInfoNode.SetAttribute("ДопИнфПВидО", additionalInfo.AdditionalOperationInfo);

            if (additionalInfo.CountryNames != null)
            {
                foreach (var countryName in additionalInfo.CountryNames)
                {
                    var countryNode = xmlDocument.CreateElement("КрНаимСтрПр"); // Краткое наименование страны происхождения товара (графа 10а счетафактуры)/страна регистрации производителя товара
                    countryNode.InnerText = countryName;

                    additionalInfoNode.AppendChild(countryNode);
                }
            }

            AddDocumentInfo(xmlDocument, additionalInfoNode, additionalInfo.SupportingDocumentList, "СопрДокТов");

            // НалУчАморт
            AddTaxAccountingAmortization(xmlDocument, additionalInfoNode, additionalInfo.TaxAccountingAmortization);

            // СумНалВосст
            AddRecoveredTaxAmount(xmlDocument, additionalInfoNode, additionalInfo.RecoveredTaxAmount);

            // СведПрослеж
            AddItemTracingInfo(xmlDocument, additionalInfo.ItemTracingInfoList, additionalInfoNode);

            // НомСредИдентТов
            AddItemIdentificationNumberInfo(xmlDocument, additionalInfo.ItemIdentificationNumberList, additionalInfoNode);

            // СвГосСист
            AddGovernmentSystemAdditionalInfo(xmlDocument, additionalInfoNode, additionalInfo.GovernmentSystemAdditionalInfoList);

            itemNode.AppendChild(additionalInfoNode);
        }

        /// <summary>
        /// СвГосСист
        /// </summary>
        private static void AddGovernmentSystemAdditionalInfo(XmlDocument xmlDocument, XmlElement parentElement, List<GovernmentSystemAdditionalInfo> governmentSystemAdditionalInfoList)
        {
            if (governmentSystemAdditionalInfoList == null || governmentSystemAdditionalInfoList.Count == 0)
            {
                return;
            }

            foreach (var item in governmentSystemAdditionalInfoList)
            {
                var itemNode = xmlDocument.CreateElement("СвГосСист"); // Дополнительные сведения о товаре, подлежащем идентификации и учету в государственной информационной системе 
                itemNode.SetAttribute("НаимГосСист", item.InformationSystemName);

                if (!string.IsNullOrEmpty(item.AccountingUnit))
                    itemNode.SetAttribute("УчетЕд", item.AccountingUnit);

                if (!string.IsNullOrEmpty(item.OtherInfo))
                    itemNode.SetAttribute("ИнаяИнф", item.OtherInfo);

                if (item.AccountingUnitIds != null)
                {
                    foreach (var id in item.AccountingUnitIds)
                    {
                        var idNode = xmlDocument.CreateElement("ИдНомУчетЕд");
                        idNode.InnerText = id;

                        itemNode.AppendChild(idNode);
                    }
                }

                parentElement.AppendChild(itemNode);
            }
        }

        /// <summary>
        /// СумНалВосст
        /// </summary>
        private static void AddRecoveredTaxAmount(XmlDocument xmlDocument, XmlElement parentElement, VatAmount vat)
        {
            if (vat == null)
            {
                return;
            }

            var taxNode = xmlDocument.CreateElement("СумНалВосст");// Сумма налога, восстановленного при передаче имущества, нематериальных активов и имущественных прав в качестве вклада в уставный капитал
            AddVatValue(xmlDocument, taxNode, vat);

            parentElement.AppendChild(taxNode);
        }

        /// <summary>
        /// НалУчАморт
        /// </summary>
        private static void AddTaxAccountingAmortization(XmlDocument xmlDocument, XmlElement parentElement, TaxAccountingAmortization taxAccountingAmortization)
        {
            if (taxAccountingAmortization == null)
            {
                return;
            }

            var accountingAmortizationNode = xmlDocument.CreateElement("НалУчАморт");
            accountingAmortizationNode.SetAttribute("АмГруппа", taxAccountingAmortization.AmortizationGroup);
            accountingAmortizationNode.SetAttribute("КодОКОФ", taxAccountingAmortization.OkofCode);
            accountingAmortizationNode.SetAttribute("СрПолИспОС", taxAccountingAmortization.EstablishedUsefulLife);
            accountingAmortizationNode.SetAttribute("ФактСрокИсп", taxAccountingAmortization.ActualUsefulLifeMonths);

            parentElement.AppendChild(accountingAmortizationNode);
        }

        /// <summary>
        /// СведПрослеж
        /// </summary>
        private static void AddItemTracingInfo(XmlDocument xmlDocument, List<InvoiceItemTracingInfo> itemTracingInfoList, XmlElement parentElement)
        {
            if (itemTracingInfoList == null || itemTracingInfoList.Count == 0)
            {
                return;
            }

            foreach (var item in itemTracingInfoList)
            {
                var itemNode = xmlDocument.CreateElement("СведПрослеж"); // Сведения о товаре, подлежащем прослеживаемости
                itemNode.SetAttribute("НомТовПрослеж", item.RegNumberUnit); // Регистрационный номер партии товаров
                itemNode.SetAttribute("ЕдИзмПрослеж", item.UnitCode); // Единица количественного учета товара, используемая в целях осуществления прослеживаемости
                
                if (!string.IsNullOrEmpty(item.UnitName))
                    itemNode.SetAttribute("НаимЕдИзмПрослеж", item.UnitName); // Наименование единицы количественного учета товара, используемой в целях осуществления прослеживаемости.

                itemNode.SetAttribute("КолВЕдПрослеж", item.Quantity.ToString(quantityToStringPattern, CultureInfo.InvariantCulture)); // Количество товара в единицах измерения прослеживаемого товара
                itemNode.SetAttribute("СтТовБезНДСПрослеж", item.SumWithoutVat.ToString("0.00", CultureInfo.InvariantCulture)); // Стоимость товара, подлежащего прослеживаемости, без налога на добавленную стоимость, в рублях (графа 14 счета-фактуры)

                if (!string.IsNullOrEmpty(item.AdditionalInfo))
                    itemNode.SetAttribute("ДопИнфПрослеж", item.AdditionalInfo); // Дополнительный показатель для идентификации товаров, подлежащих прослеживаемости

                parentElement.AppendChild(itemNode);
            }
        }

        /// <summary>
        /// НомСредИдентТов
        /// </summary>
        private static void AddItemIdentificationNumberInfo(XmlDocument xmlDocument, List<InvoiceItemIdentificationNumber> itemIdentificationNumberList, XmlElement parentElement)
        {
            if (itemIdentificationNumberList == null || itemIdentificationNumberList.Count == 0)
            {
                return;
            }

            foreach (var identificationNumber in itemIdentificationNumberList)
            {
                var rfidNode = xmlDocument.CreateElement("НомСредИдентТов"); // Номер средств идентификации товаров
                if (!string.IsNullOrEmpty(identificationNumber.PackageId))
                    rfidNode.SetAttribute("ИдентТрансУпак", identificationNumber.PackageId); // Уникальный идентификатор транспортной упаковки

                if (!string.IsNullOrEmpty(identificationNumber.MarkedProductQuantity))
                    rfidNode.SetAttribute("КолВедМарк", identificationNumber.MarkedProductQuantity); // Количество товара в единицах измерения маркированного товара средствами идентификации

                if (!string.IsNullOrEmpty(identificationNumber.ProductionBatchCode))
                    rfidNode.SetAttribute("ПрПартМарк", identificationNumber.ProductionBatchCode); // Производственная партия (КОД)

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
        /// СвДТ
        /// </summary>
        private static void AddCustomDeclarationsInformation(XmlDocument xmlDocument, InvoiceItem item, XmlElement itemNode)
        {
            if (item.CustomsDeclarationList == null)
            {
                return;
            }

            foreach (var customDeclaration in item.CustomsDeclarationList)
            {
                var declaration = xmlDocument.CreateElement("СвДТ"); // Сведения о декларации на товары

                if (!string.IsNullOrEmpty(customDeclaration.CountryCode))
                    declaration.SetAttribute("КодПроисх", customDeclaration.CountryCode); // Цифровой код страны происхождения товара (Графа 10 счета-фактуры)

                if (!string.IsNullOrEmpty(customDeclaration.DeclarationNumber))
                    declaration.SetAttribute("НомерДТ", customDeclaration.DeclarationNumber); // Регистрационный номер декларации на товары (графа 11 счета-фактуры)

                itemNode.AppendChild(declaration);
            }
        }

        private static void AddVatValue(XmlDocument xmlDocument, bool isWithoutVat, decimal? vat, XmlElement parentTaxNode)
            => AddVatValue(xmlDocument, parentTaxNode, new VatAmount { Amount = vat, WithoutVat = isWithoutVat });

        private static void AddVatValue(XmlDocument xmlDocument, XmlElement parentTaxNode, VatAmount vatAmount)
        {
            if (vatAmount.WithoutVat)
            {
                var withoutVatNode = xmlDocument.CreateElement("БезНДС");
                withoutVatNode.InnerText = "без НДС";

                parentTaxNode.AppendChild(withoutVatNode);
            }
            else
            {
                if (vatAmount.Amount == null)
                    throw new ArgumentNullException("Не указана сумма НДС!");

                var taxInfoNode = xmlDocument.CreateElement("СумНал");
                taxInfoNode.InnerText = vatAmount.Amount.Value.ToString("0.00", CultureInfo.InvariantCulture);

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

            // СвПер
            AddTransferDetails(xmlDocument, transferNode, transferInfo.TransferDetails);

            // ИнфПолФХЖ3
            AddOtherEconomicInfo(xmlDocument, transferNode, transferInfo.OtherEconomicInfo, "ИнфПолФХЖ3");

            parentElement.AppendChild(transferNode);
        }

        private static void AddTransferDetails(XmlDocument xmlDocument, XmlElement transferNode, TransferDetails transferDetails)
        {
            if (transferDetails == null)
                return;

            var transferInfoNode = xmlDocument.CreateElement("СвПер"); // Сведения о передаче (сдаче) товаров (результатов работ), имущественных прав(о предъявлении оказанных услуг)
            transferInfoNode.SetAttribute("СодОпер", transferDetails.OperationName); // Содержание операции

            if (!string.IsNullOrEmpty(transferDetails.OperationType))
                transferInfoNode.SetAttribute("ВидОпер", transferDetails.OperationType); // Вид операции

            if (transferDetails.Date != null)
                transferInfoNode.SetAttribute("ДатаПер", transferDetails.Date.Value.ToString("dd.MM.yyyy")); // Дата отгрузки товаров (передачи результатов работ), передачи имущественных прав (предъявления оказанных услуг)

            if (transferDetails.StartDate != null)
                transferInfoNode.SetAttribute("ДатаНачПер", transferDetails.StartDate.Value.ToString("dd.MM.yyyy")); // Дата начала периода оказания услуг (выполнения работ, поставки товаров)

            if (transferDetails.EndDate != null)
                transferInfoNode.SetAttribute("ДатаОконПер", transferDetails.EndDate.Value.ToString("dd.MM.yyyy")); // Дата окончания периода оказания услуг (выполнения работ, поставки товаров)

            if (transferDetails.TransferDocuments?.Count > 0)
            {
                AddDocumentInfo(xmlDocument, transferInfoNode, transferDetails.TransferDocuments, "ОснПер");
            }
            else
            {
                var noDocNode = xmlDocument.CreateElement("БезДокОснПер");

                noDocNode.InnerText = "1";

                transferInfoNode.AppendChild(noDocNode);
            }

            if (transferDetails.SenderPerson is SenderPerson sender)
            {
                var senderNode = xmlDocument.CreateElement("СвЛицПер"); // Сведения о лице, передавшем товар (груз)
                if (sender.Employee is SenderEmployee senderEmployee)
                {
                    var senderEmployeeNode = xmlDocument.CreateElement("РабОргПрод"); // Работник организации продавца
                    senderEmployeeNode.SetAttribute("Должность", senderEmployee.JobTitle); // Должность

                    if (!string.IsNullOrEmpty(senderEmployee.OtherInfo))
                        senderEmployeeNode.SetAttribute("ИныеСвед", senderEmployee.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                    AddPersonName(xmlDocument, senderEmployee, senderEmployeeNode);

                    senderNode.AppendChild(senderEmployeeNode);
                }
                else if (sender.OtherIssuer is OtherIssuer senderOtherIssue)
                {
                    var senderOtherNode = xmlDocument.CreateElement("ИнЛицо"); // Сведения о лице, передавшем товар (груз)
                    
                    if (senderOtherIssue.OrganizationPerson is TransferOrganizationPerson organizationPerson)
                    {
                        var senderOtherIssueEmployeeNode = xmlDocument.CreateElement("ПредОргПер"); // Представитель организации, которой доверена отгрузка товаров
                        senderOtherIssueEmployeeNode.SetAttribute("Должность", organizationPerson.JobTitle); // Должность

                        if (!string.IsNullOrEmpty(organizationPerson.OtherInfo))
                            senderOtherIssueEmployeeNode.SetAttribute("ИныеСвед", organizationPerson.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                        senderOtherIssueEmployeeNode.SetAttribute("НаимОргПер", organizationPerson.OrganizationName); // Наименование организации

                        if (!string.IsNullOrEmpty(organizationPerson.OrganizationInn))
                            senderOtherIssueEmployeeNode.SetAttribute("ИННЮЛПер", organizationPerson.OrganizationInn);

                        if (organizationPerson.OrganizationBase is EDM.Models.V5_03.Document orgDocument)
                            AddDocumentInfo(xmlDocument, senderOtherIssueEmployeeNode, [orgDocument], "ОснДоверОргПер"); // Основание, по которому организации доверена отгрузка товаров

                        if (organizationPerson.EmployeeBase is EDM.Models.V5_03.Document empDocument)
                            AddDocumentInfo(xmlDocument, senderOtherIssueEmployeeNode, [empDocument], "ОснПолнПредПер"); // Основание полномочий представителя организации на отгрузку товаров

                        AddPersonName(xmlDocument, organizationPerson, senderOtherIssueEmployeeNode);

                        senderOtherNode.AppendChild(senderOtherIssueEmployeeNode);
                    }
                    else if (senderOtherIssue.PhysicalPerson is TransferPhysicalPerson physicalPerson)
                    {
                        var senderOtherIssuePersonNode = xmlDocument.CreateElement("ФЛПер"); // Представитель организации, которой доверена отгрузка товаров

                        if (!string.IsNullOrEmpty(physicalPerson.PersonInn))
                            senderOtherIssuePersonNode.SetAttribute("ИННФЛПер", physicalPerson.PersonInn); // ИНН физического лица, в том числе индивидуального 

                        if (!string.IsNullOrEmpty(physicalPerson.Othernfo))
                            senderOtherIssuePersonNode.SetAttribute("ИныеСвед", physicalPerson.Othernfo); // Иные сведения, идентифицирующие физическое лицо

                        if (physicalPerson.PersonBase is EDM.Models.V5_03.Document persDocument)
                            AddDocumentInfo(xmlDocument, senderOtherIssuePersonNode, [persDocument], "ОснДоверФЛ"); // Основание, по которому физическому лицу доверена отгрузка товаров (передача результатов работ), передача имущественных прав (предъявление оказанных услуг)

                        AddPersonName(xmlDocument, physicalPerson, senderOtherIssuePersonNode);

                        senderOtherNode.AppendChild(senderOtherIssuePersonNode);
                    }
                    else
                    {
                        throw new ArgumentNullException("Не указаны сведения о ином лице, передавшем товар (ИнЛицо).");
                    }

                    senderNode.AppendChild(senderOtherNode);
                }
                else
                {
                    throw new ArgumentNullException("Не указаны сведения о лице, передавшем товар (СвЛицПер).");
                }

                transferInfoNode.AppendChild(senderNode);
            }

            var transportationNode = xmlDocument.CreateElement("Тран"); // Транспортировка
            if (transferDetails.Transportation is Transportation transportation)
            {
                if (!string.IsNullOrEmpty(transportation.TransferTextInfo))
                    transportationNode.SetAttribute("СвТран", transportation.TransferTextInfo); // Сведения о транспортировке и грузе

                if (!string.IsNullOrEmpty(transportation.Incoterms))
                    transportationNode.SetAttribute("Инкотермс", transportation.Incoterms);

                if (!string.IsNullOrEmpty(transportation.IncotermsVersion))
                    transportationNode.SetAttribute("ВерИнкотермс", transportation.IncotermsVersion);

                transferInfoNode.AppendChild(transportationNode);
            }

            if (transferDetails.CreatedThingInfo is CreatedThingTransferInfo createdThingInfo)
            {
                var createdThingInfoNode = xmlDocument.CreateElement("СвПерВещи"); // Сведения о передаче вещи, изготовленной по договору подряда

                if (createdThingInfo.Date != null)
                    createdThingInfoNode.SetAttribute("ДатаПерВещ", createdThingInfo.Date.Value.ToString("dd.MM.yyyy")); // Дата передачи вещи, изготовленной по договору подряда

                if (!string.IsNullOrEmpty(createdThingInfo.Information))
                    createdThingInfoNode.SetAttribute("СвПерВещ", createdThingInfo.Information); // Сведения о передаче

                if (createdThingInfo.Document is EDM.Models.V5_03.Document createdThingInfoDocument)
                    AddDocumentInfo(xmlDocument, createdThingInfoNode, [createdThingInfoDocument], "ДокПерВещ");

                transferInfoNode.AppendChild(createdThingInfoNode);
            }

            transferNode.AppendChild(transferInfoNode);
        }

        private static void AddPersonName(XmlDocument xmlDocument, Person person, XmlElement parentNode)
        {
            var personNode = xmlDocument.CreateElement("ФИО");
            personNode.SetAttribute("Фамилия", person.Surname);
            personNode.SetAttribute("Имя", person.FirstName);

            if (!string.IsNullOrEmpty(person.Patronymic))
                personNode.SetAttribute("Отчество", person.Patronymic);

            parentNode.AppendChild(personNode);
        }

        /// <summary>
        /// Реквизиты документа (РеквДокТип).
        /// </summary>
        private static void AddDocumentInfo(XmlDocument xmlDocument, XmlElement parentElement, List<EDM.Models.V5_03.Document> documentList, string elementName)
        {
            if (documentList == null || documentList.Count == 0)
            {
                return;
            }

            foreach (var document in documentList)
            {
                var documentInfoNode = xmlDocument.CreateElement(elementName); // Реквизиты документа, подтверждающего отгрузку товаров (работ, услуг, имущественных прав) (графа 5а счёт-фактуры)

                documentInfoNode.SetAttribute("РеквНаимДок", document.Name); // Наименование документа об отгрузке
                documentInfoNode.SetAttribute("РеквНомерДок", document.Number); // Номер документа об отгрузке
                documentInfoNode.SetAttribute("РеквДатаДок", document.Date.ToString("dd.MM.yyyy")); // Дата документа об отгрузке

                if (!string.IsNullOrEmpty(document.FileId))
                    documentInfoNode.SetAttribute("РеквИдФайлДок", document.FileId);

                if (!string.IsNullOrEmpty(document.DocumentId))
                    documentInfoNode.SetAttribute("РеквИдДок", document.DocumentId);

                if (!string.IsNullOrEmpty(document.StorageSystemId))
                    documentInfoNode.SetAttribute("РИдСистХранД", document.StorageSystemId);

                if (!string.IsNullOrEmpty(document.SystemUrl))
                    documentInfoNode.SetAttribute("РеквУРЛСистДок", document.SystemUrl);

                if (!string.IsNullOrEmpty(document.OtherInfo))
                    documentInfoNode.SetAttribute("РеквДопСведДок", document.OtherInfo);

                if (document.Creators != null)
                {
                    foreach (var creator in document.Creators)
                    {
                        // Идентифицирующие реквизиты экономических субъектов, составивших (сформировавших) документ
                        var creatorNode = xmlDocument.CreateElement("РеквИдРекСост");
                        if (!string.IsNullOrEmpty(creator.InnLegalEntity))
                        {
                            var innNode = xmlDocument.CreateElement("ИННЮЛ");
                            innNode.InnerText = creator.InnLegalEntity;

                            creatorNode.AppendChild(innNode);
                        }
                        else if (!string.IsNullOrEmpty(creator.InnIndividual))
                        {
                            var innNode = xmlDocument.CreateElement("ИННФЛ");
                            innNode.InnerText = creator.InnIndividual;

                            creatorNode.AppendChild(innNode);
                        }
                        else if (creator.ForeignEntity is ForeignEntity organization)
                        {
                            AddForeignEntity(xmlDocument, creatorNode, organization, "ДаннИно");
                        }
                        else if (!string.IsNullOrEmpty(creator.AuthorityShortName))
                        {
                            var authorityNode = xmlDocument.CreateElement("НаимОИВ");
                            authorityNode.InnerText = creator.AuthorityShortName;

                            creatorNode.AppendChild(authorityNode);
                        }

                        documentInfoNode.AppendChild(creatorNode);
                    }
                }

                parentElement.AppendChild(documentInfoNode);
            }
        }

        private static void AddForeignEntity(XmlDocument xmlDocument, XmlElement creatorNode, ForeignEntity organization, string elementName)
        {
            var foreignNode = xmlDocument.CreateElement(elementName);
            foreignNode.SetAttribute("ИдСтат", organization.Status);
            foreignNode.SetAttribute("КодСтр", organization.CountryCode);
            foreignNode.SetAttribute("НаимСтран", organization.CountryName);
            foreignNode.SetAttribute("Наим", organization.Name);

            if (!string.IsNullOrEmpty(organization.Identifier))
                foreignNode.SetAttribute("Идентиф", organization.Identifier);

            if (!string.IsNullOrEmpty(organization.OtherInfo))
                foreignNode.SetAttribute("ИныеСвед", organization.OtherInfo);

            creatorNode.AppendChild(foreignNode);
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

                if (!string.IsNullOrEmpty(signer.JobTitle))
                    signerNode.SetAttribute("Должн", signer.JobTitle); // Должность

                if (signer.SignatureType != null)
                    signerNode.SetAttribute("ТипПодпис", ((int)signer.SignatureType).ToString()); // Тип подписи

                if (signer.SigningDate != null)
                    signerNode.SetAttribute("ДатаПодДок", signer.SigningDate.Value.ToString("dd.MM.yyyy"));// Дата подписания документа

                signerNode.SetAttribute("СпосПодтПолном", ((int)signer.AuthorityConfirmationType).ToString()); // Способ подтверждения полномочий представителя на подписание документа

                if (signer.AdditionalInfo != null)
                    signerNode.SetAttribute("ДопСведПодп", signer.AdditionalInfo); // Дополнительные сведения о подписанте

                AddPersonName(xmlDocument, signer.Person, signerNode);

                if (signer.ElectronicPoAInfo is ElectronicPoAInfo epoaInfo)
                {
                    var epoaNode = xmlDocument.CreateElement("СвДоверЭл");
                    epoaNode.SetAttribute("НомДовер", epoaInfo.RegistrationNumber.ToString("D"));
                    epoaNode.SetAttribute("ДатаВыдДовер", epoaInfo.IssueDate.ToString("dd.MM.yyyy"));

                    if (!string.IsNullOrEmpty(epoaInfo.InternalNumber))
                        epoaNode.SetAttribute("ВнНомДовер", epoaInfo.InternalNumber);

                    if (epoaInfo.InternalRegistrationDate != null)
                        epoaNode.SetAttribute("ДатаВнРегДовер", epoaInfo.InternalRegistrationDate.Value.ToString("dd.MM.yyyy"));

                    epoaNode.SetAttribute("ИдСистХран", epoaInfo.SystemIdentification);

                    if (!string.IsNullOrEmpty(epoaInfo.SystemURL))
                        epoaNode.SetAttribute("УРЛСист", epoaInfo.SystemURL);

                    signerNode.AppendChild(epoaNode);
                }
                else if (signer.PaperPoAInfo is PaperPoAInfo ppoaInfo)
                {
                    var ppoaNode = xmlDocument.CreateElement("СвДоверБум");
                    ppoaNode.SetAttribute("ДатаВыдДовер", ppoaInfo.IssueDate.ToString("dd.MM.yyyy"));
                    ppoaNode.SetAttribute("ВнНомДовер", ppoaInfo.InternalNumber);

                    if (!string.IsNullOrEmpty(ppoaInfo.TrusteeInfo))
                        ppoaNode.SetAttribute("СвИдДовер", ppoaInfo.TrusteeInfo);

                    if (ppoaInfo.Signer != null)
                        AddPersonName(xmlDocument, ppoaInfo.Signer, ppoaNode);

                    signerNode.AppendChild(ppoaNode);
                }

                parentElement.AppendChild(signerNode);
            }
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
                var formattedXml = (doc.Declaration != null ? doc.Declaration + Environment.NewLine : string.Empty) + doc.ToString();

                return formattedXml;
            }
            catch
            {
                return xmlDocument.OuterXml;
            }
        }
    }
}
