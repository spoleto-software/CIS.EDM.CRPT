using System;
using System.Globalization;
using System.Xml;
using CIS.EDM.CRPT.Models;
using CIS.EDM.Models;
using CIS.EDM.Models.Buyer;

namespace CIS.EDM.CRPT.Helpers
{
    /// <summary>
    /// Формирование файла обмена информации покупателя.
    /// </summary>
    internal static partial class XmlHelper
    {
        internal static XmlDocumentInfo GenerateXml(BuyerUniversalTransferDocument buyerDataContract)
        {
            if (buyerDataContract.SenderEdmParticipant == null)
                throw new ArgumentNullException("Не указан идентификатор отправителя УПД.");

            if (buyerDataContract.RecipientEdmParticipant == null)
                throw new ArgumentNullException("Не указан идентификатор получателя УПД.");

            if (buyerDataContract.DocumentCreator == null)
                throw new ArgumentNullException("Не указан экономический субъект - составитель файла обмена счета-фактуры");

            var xmlDocument = CreateXmlDocument(buyerDataContract);
            GenerateXmlFrom(xmlDocument, buyerDataContract);

            var xml = MakeXmlFormatted(xmlDocument);

            return new XmlDocumentInfo
            {
                Id = buyerDataContract.FileId,
                Content = xml,
                FileEncoding = DefaultEncoding
            };
        }

        private static void GenerateXmlFrom(XmlDocument xmlDocument, BuyerUniversalTransferDocument buyerDataContract)
        {
            var headerNode = xmlDocument.CreateElement("СвУчДокОбор");
            headerNode.SetAttribute("ИдОтпр", buyerDataContract.SenderEdmParticipant.FullId);
            headerNode.SetAttribute("ИдПол", buyerDataContract.RecipientEdmParticipant.FullId);
            xmlDocument.DocumentElement.AppendChild(headerNode);

            var operatorNode = xmlDocument.CreateElement("СвОЭДОтпр");
            operatorNode.SetAttribute("НаимОрг", buyerDataContract.EdmOperator.Name);
            operatorNode.SetAttribute("ИННЮЛ", buyerDataContract.EdmOperator.Inn);
            operatorNode.SetAttribute("ИдЭДО", buyerDataContract.EdmOperator.OperatorId);
            headerNode.AppendChild(operatorNode);

            var documentNode = xmlDocument.CreateElement("ИнфПок"); // Информация покупателя
            documentNode.SetAttribute("КНД", buyerDataContract.TaxDocumentCode);
            documentNode.SetAttribute("ДатаИнфПок", buyerDataContract.DateCreation.ToString("dd.MM.yyyy")); // Дата формирования файла обмена информации покупателя
            documentNode.SetAttribute("ВремИнфПок", buyerDataContract.DateCreation.ToString("HH.mm.ss")); // Время формирования файла обмена информации покупателя
            documentNode.SetAttribute("НаимЭконСубСост", GetEconomicEntityName(buyerDataContract.DocumentCreator)); // Наименование экономического субъекта - составителя файла обмена информации покупателя
            if (!String.IsNullOrEmpty(buyerDataContract.DocumentCreatorBase))
                documentNode.SetAttribute("ОснДоверОргСост", buyerDataContract.DocumentCreatorBase); // Основание, по которому экономический субъект является составителем файла обмена счета-фактуры (информации продавца)

            xmlDocument.DocumentElement.AppendChild(documentNode);

            var sellerDocumentInfo = buyerDataContract.SellerDocumentInfo;
            var sellerFileNode = xmlDocument.CreateElement("ИдИнфПрод");// Идентификация файла обмена счета-фактуры (информации продавца) или файла обмена информации продавца
            sellerFileNode.SetAttribute("ИдФайлИнфПр", sellerDocumentInfo.FileId); // Идентификатор файла обмена информации продавца. Содержит (повторяет) имя файла обмена счета-фактуры (информации продавца) или файла обмена информации продавца (без расширения)
            sellerFileNode.SetAttribute("ДатаФайлИнфПр", sellerDocumentInfo.DateCreation); // Дата формирования файла обмена информации продавца. Указывается (повторяет) значение ДатаИнфПр, указанное в файле обмена счета-фактуры (информации продавца) или файле обмена информации продавца
            sellerFileNode.SetAttribute("ВремФайлИнфПр", sellerDocumentInfo.TimeCreation); // Время формирования файла обмена информации продавца. Указывается (повторяет) значение ВремИнфПр, указанное в файле обмена счета-фактуры (информации продавца) или файле обмена информации продавца

            foreach (var eds in sellerDocumentInfo.EdsBodyList)
            {
                var edsNode = xmlDocument.CreateElement("ЭП"); // Электронная подпись файла обмена информации продавца. Представляется в кодировке Base64
                edsNode.InnerText = eds;
                sellerFileNode.AppendChild(edsNode);
            }
            documentNode.AppendChild(sellerFileNode);

            AddEconomicLife4(xmlDocument, buyerDataContract, documentNode);

            AddSellerInfoCircumPublicProc(xmlDocument, buyerDataContract, documentNode);

            AddBuyerSigners(xmlDocument, buyerDataContract, documentNode);
        }

        /// <summary>
        /// Информация покупателя об обстоятельствах закупок для государственных и муниципальных нужд (для учета Федеральным казначейством денежных обязательств)
        /// </summary>
        /// <remarks>ИнфПокГосЗакКазн</remarks>
        private static void AddSellerInfoCircumPublicProc(XmlDocument xmlDocument, BuyerUniversalTransferDocument dataContract, XmlElement parentElement)
        {
            var buyerInfoCircum = dataContract.BuyerInfoCircumPublicProc;
            if (dataContract.BuyerInfoCircumPublicProc == null)
                return;

            var circumPublicProcNode = xmlDocument.CreateElement("ИнфПокГосЗакКазн");

            if (!String.IsNullOrEmpty(buyerInfoCircum.PurchasingIdentificationCode))
                circumPublicProcNode.SetAttribute("ИдКодЗак", buyerInfoCircum.PurchasingIdentificationCode);

            circumPublicProcNode.SetAttribute("ЛицСчетПок", buyerInfoCircum.PersonalAccountBuyer);
            circumPublicProcNode.SetAttribute("НаимФинОргПок", buyerInfoCircum.BuyerFinancialAuthorityName);
            circumPublicProcNode.SetAttribute("НомРеестрЗапПок", buyerInfoCircum.BuyerBudgetRegisterNumber);

            if (!String.IsNullOrEmpty(buyerInfoCircum.BuyerBudgetObligationAccountNumber))
                circumPublicProcNode.SetAttribute("УчНомБюдОбязПок", buyerInfoCircum.BuyerBudgetObligationAccountNumber);

            if (!String.IsNullOrEmpty(buyerInfoCircum.BuyerTreasuryCode))
                circumPublicProcNode.SetAttribute("КодКазначПок", buyerInfoCircum.BuyerTreasuryCode);

            if (!String.IsNullOrEmpty(buyerInfoCircum.BuyerTreasuryName))
                circumPublicProcNode.SetAttribute("НаимКазначПок", buyerInfoCircum.BuyerTreasuryName);

            circumPublicProcNode.SetAttribute("ОКТМОПок", buyerInfoCircum.BuyerMunicipalCode);

            if (!String.IsNullOrEmpty(buyerInfoCircum.DeliveryMunicipalCode))
                circumPublicProcNode.SetAttribute("ОКТМОМесПост", buyerInfoCircum.DeliveryMunicipalCode);

            if (buyerInfoCircum.PaymentDate != null)
                circumPublicProcNode.SetAttribute("ДатаОплПред", buyerInfoCircum.PaymentDate.Value.ToString("dd.MM.yyyy"));

            if (!String.IsNullOrEmpty(buyerInfoCircum.FinancialObligationAccountNumber))
                circumPublicProcNode.SetAttribute("УчНомДенОбяз", buyerInfoCircum.FinancialObligationAccountNumber);

            if (!String.IsNullOrEmpty(buyerInfoCircum.PaymentOrder))
                circumPublicProcNode.SetAttribute("ОчерПлат", buyerInfoCircum.PaymentOrder);

            if (buyerInfoCircum.PaymentType != EDM.Models.Buyer.Reference.PaymentType.NotSpecified)
                circumPublicProcNode.SetAttribute("ВидПлат", ((int)buyerInfoCircum.PaymentType).ToString());

            foreach (var obligationInfo in buyerInfoCircum.FinancialObligationInfoList)
            {
                var circumPublicProcInfoNode = xmlDocument.CreateElement("ИнфСведДенОбяз");
                circumPublicProcInfoNode.SetAttribute("НомСтр", obligationInfo.RowNumber.ToString());

                if (!String.IsNullOrEmpty(obligationInfo.ObjectFAIPCode))
                    circumPublicProcInfoNode.SetAttribute("КодОбъектФАИП", obligationInfo.ObjectFAIPCode);

                if (obligationInfo.FundType != EDM.Models.Buyer.Reference.FundType.NotSpecified)
                    circumPublicProcInfoNode.SetAttribute("ВидСредств", ((int)obligationInfo.FundType).ToString());

                circumPublicProcInfoNode.SetAttribute("КодПокБюджКласс", obligationInfo.BuyerBudgetClassCode);

                if (!String.IsNullOrEmpty(obligationInfo.BuyerTargetCode))
                    circumPublicProcInfoNode.SetAttribute("КодЦелиПокуп", obligationInfo.BuyerTargetCode);

                circumPublicProcInfoNode.SetAttribute("СумАванс", obligationInfo.AmountAdvance.ToString("0.00", CultureInfo.InvariantCulture));

                circumPublicProcNode.AppendChild(circumPublicProcInfoNode);
            }

            parentElement.AppendChild(circumPublicProcNode);
        }

        /// <summary>
        /// Содержание факта хозяйственной жизни 4 - сведения о принятии товаров (результатов выполненных работ), имущественных прав (о подтверждении факта оказания услуг)
        /// </summary>
        /// <remarks>СодФХЖ4</remarks>
        private static void AddEconomicLife4(XmlDocument xmlDocument, BuyerUniversalTransferDocument dataContract, XmlElement parentElement)
        {
            var sellerDocumentInfo = dataContract.SellerDocumentInfo;

            var documentInfoNode = xmlDocument.CreateElement("СодФХЖ4"); // Содержание факта хозяйственной жизни 4 - сведения о принятии товаров (результатов выполненных работ), имущественных прав (о подтверждении факта оказания услуг)
            documentInfoNode.SetAttribute("НаимДокОпрПр", sellerDocumentInfo.DocumentName); // Наименование первичного документа, согласованное сторонами сделки. Указывается (повторяет) значение НаимДокОпр, указанное в файле обмена счета-фактуры (информации продавца) или файле обмена информации продавца
            documentInfoNode.SetAttribute("Функция", sellerDocumentInfo.Function.ToString()); // Указывается (повторяет) значение Функция, указанное в файле обмена счета-фактуры (информации продавца) или файле обмена информации продавца
            documentInfoNode.SetAttribute("НомСчФИнфПр", sellerDocumentInfo.DocumentNumber); // Номер счета-фактуры (информации продавца). Номер поступившего на подпись документа об отгрузке товаров (выполнении работ), передаче имущественных прав (об оказании услуг). Указывается (повторяет) значение НомерСчФ, указанное в файле обмена счета-фактуры (информации продавца) или файле обмена информации продавца
            documentInfoNode.SetAttribute("ДатаСчФИнфПр", sellerDocumentInfo.DocumentDate); // Дата составления (выписки) счета-фактуры (информации продавца). Дата поступившего на подпись документа об отгрузке товаров (выполнении работ), передаче имущественных прав (об оказании услуг). Указывается (повторяет) значение ДатаСчФ, указанное в файле обмена счета-фактуры (информации продавца) или файле обмена информации продавца

            if (!String.IsNullOrEmpty(dataContract.OperationType))
                documentInfoNode.SetAttribute("ВидОперации", dataContract.OperationType); // Дополнительная информация, позволяющая в автоматизированном режиме определять необходимый для конкретного случая порядок использования информации документа у покупателя

            parentElement.AppendChild(documentInfoNode);

            var confirmNode = xmlDocument.CreateElement("СвПрин"); // Сведения о принятии товаров (результатов выполненных работ), имущественных прав (о подтверждении факта оказания услуг)
            var acceptanceInfo = dataContract.AcceptanceInfo;
            
            if (!String.IsNullOrEmpty(acceptanceInfo.OperationName))
                confirmNode.SetAttribute("СодОпер", acceptanceInfo.OperationName); // Содержание операции (текст)

            if (acceptanceInfo.Date != null)
                confirmNode.SetAttribute("ДатаПрин", acceptanceInfo.Date.Value.ToString("dd.MM.yyyy")); // Дата принятия товаров (результатов выполненных работ), имущественных прав (подтверждения факта оказания услуг)

            documentInfoNode.AppendChild(confirmNode);

            var operationNameInfo = acceptanceInfo.OperationNameInfo;
            if (operationNameInfo != null)
            {
                var confirmInfoNode = xmlDocument.CreateElement("КодСодОпер"); // Код содержания операции
                confirmInfoNode.SetAttribute("КодИтога", ((int)operationNameInfo.Code).ToString()); // Код, обозначающий итог приемки товара (работ, услуг, прав).

                if (!String.IsNullOrEmpty(operationNameInfo.DiscrepancyDocumentName))
                    confirmNode.SetAttribute("НаимДокРасх", operationNameInfo.DiscrepancyDocumentName); // Наименование документа, оформляющего расхождения

                if (operationNameInfo.DiscrepancyDocumentCode != EDM.Models.Buyer.Reference.DiscrepancyDocumentCode.NotSpecified)
                    confirmNode.SetAttribute("ВидДокРасх", ((int)operationNameInfo.DiscrepancyDocumentCode).ToString()); // Код вида документа о расхождениях

                if (!String.IsNullOrEmpty(operationNameInfo.DiscrepancyDocumentNumber))
                    confirmNode.SetAttribute("НомДокРасх", operationNameInfo.DiscrepancyDocumentNumber); // Номер документа покупателя о расхождениях

                if (operationNameInfo.DiscrepancyDocumentDate != null)
                    confirmNode.SetAttribute("ДатаДокРасх", operationNameInfo.DiscrepancyDocumentDate.Value.ToString("dd.MM.yyyy")); // Дата документа о расхождениях

                if (!String.IsNullOrEmpty(operationNameInfo.DiscrepancyDocumentId))
                    confirmNode.SetAttribute("ИдФайлДокРасх", operationNameInfo.DiscrepancyDocumentId); // Идентификатор файла обмена документа о расхождениях, сформированного покупателем

                confirmNode.AppendChild(confirmInfoNode);

                if (acceptanceInfo.ReceiverPerson != null)
                {
                    var receiver = acceptanceInfo.ReceiverPerson;

                    var receiverNode = xmlDocument.CreateElement("СвЛицПрин"); // Сведения о лице, принявшем товары (груз)
                    if (receiver.Employee is ReceiverEmployee receiverEmployee)
                    {
                        var receiverEmployeeNode = xmlDocument.CreateElement("РабОргПок"); // Работник организации продавца
                        receiverEmployeeNode.SetAttribute("Должность", receiverEmployee.JobTitle); // Должность

                        if (!String.IsNullOrEmpty(receiverEmployee.EmployeeInfo))
                            receiverEmployeeNode.SetAttribute("ИныеСвед", receiverEmployee.EmployeeInfo); // Иные сведения, идентифицирующие физическое лицо

                        if (!String.IsNullOrEmpty(receiverEmployee.EmployeeBase))
                            receiverEmployeeNode.SetAttribute("ОснПолн", receiverEmployee.EmployeeBase); // Основание полномочий (доверия)

                        receiverNode.AppendChild(receiverEmployeeNode);

                        AddPersonName(xmlDocument, receiverEmployee, receiverEmployeeNode);
                    }
                    else if (receiver.OtherIssuer is OtherIssuer receiverOtherIssue)
                    {
                        if (receiverOtherIssue.OrganizationPerson is TransferOrganizationPerson organizationPerson)
                        {
                            var receiverOtherIssueEmployeeNode = xmlDocument.CreateElement("ПредОргПрин"); // Представитель организации, которой доверено принятие товаров (груза)
                            receiverOtherIssueEmployeeNode.SetAttribute("Должность", organizationPerson.JobTitle); // Должность

                            if (!String.IsNullOrEmpty(organizationPerson.EmployeeInfo))
                                receiverOtherIssueEmployeeNode.SetAttribute("ИныеСвед", organizationPerson.EmployeeInfo); // Иные сведения, идентифицирующие физическое лицо

                            receiverOtherIssueEmployeeNode.SetAttribute("НаимОргПрин", organizationPerson.OrganizationName); // Наименование организации

                            if (!String.IsNullOrEmpty(organizationPerson.OrganizationBase))
                                receiverOtherIssueEmployeeNode.SetAttribute("ОснДоверОргПрин", organizationPerson.OrganizationBase); // Основание, по которому организации доверено принятие товаров (груза)

                            if (!String.IsNullOrEmpty(organizationPerson.EmployeeBase))
                                receiverOtherIssueEmployeeNode.SetAttribute("ОснПолнПредПрин", organizationPerson.EmployeeBase); // Основание полномочий представителя организации на принятие товаров (груза)

                            receiverNode.AppendChild(receiverOtherIssueEmployeeNode);

                            AddPersonName(xmlDocument, organizationPerson, receiverOtherIssueEmployeeNode);
                        }
                        else if (receiverOtherIssue.PhysicalPerson is TransferPhysicalPerson physicalPerson)
                        {
                            var receiverOtherIssuePersonNode = xmlDocument.CreateElement("ФЛПрин"); // Представитель организации, которой доверена отгрузка товаров

                            if (!String.IsNullOrEmpty(physicalPerson.PersonInfo))
                                receiverOtherIssuePersonNode.SetAttribute("ИныеСвед", physicalPerson.PersonInfo); // Иные сведения, идентифицирующие физическое лицо

                            if (!String.IsNullOrEmpty(physicalPerson.PersonBase))
                                receiverOtherIssuePersonNode.SetAttribute("ОснДоверФЛ", physicalPerson.PersonBase); // Основание, по которому физическому лицу доверена отгрузка товаров

                            receiverNode.AppendChild(receiverOtherIssuePersonNode);

                            AddPersonName(xmlDocument, physicalPerson, receiverOtherIssuePersonNode);
                        }
                        else
                        {
                            throw new ArgumentNullException("Не указаны сведения о ином лице, принявшем товар (ИнЛицо).");
                        }
                    }
                    else
                    {
                        throw new ArgumentNullException("Не указаны сведения о лице, принявшем товар (СвЛицПрин).");
                    }

                    confirmNode.AppendChild(receiverNode);
                }
            }

            AddOtherEconomicInfo(xmlDocument, documentInfoNode, dataContract.OtherEconomicInfo, "ИнфПолФХЖ4"); // Информационное поле факта хозяйственной жизни 4
        }

        private static void AddBuyerSigners(XmlDocument xmlDocument, BuyerUniversalTransferDocument dataContract, XmlElement parentElement)
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

                AddBuyerSignerPersonInfo(xmlDocument, signer, signerNode);

                parentElement.AppendChild(signerNode);
            }
        }

        private static void AddBuyerSignerPersonInfo(XmlDocument xmlDocument, BuyerSigner signerInfo, XmlElement parentNode)
        {
            XmlElement signerPersonNode;
            if (signerInfo.LegalPersonRepresentative is LegalPersonRepresentative legalPersonRepresentative)
            {
                signerPersonNode = xmlDocument.CreateElement("ЮЛ"); // Представитель юридического лица

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
    }
}
