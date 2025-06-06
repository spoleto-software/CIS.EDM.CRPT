using System;
using System.Globalization;
using System.Xml;
using CIS.EDM.Models;
using CIS.EDM.Models.V5_03;
using CIS.EDM.Models.V5_03.Buyer;

namespace CIS.EDM.CRPT.Helpers
{
    /// <summary>
    /// Формирование файла обмена информации покупателя.
    /// </summary>
    internal static partial class XmlHelperV5_03
    {
        internal static DocumentData GenerateXml(BuyerUniversalTransferDocument buyerDataContract)
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

            return new DocumentData
            {
                Id = buyerDataContract.FileId,
                Content = xml,
                FileEncoding = DefaultEncoding
            };
        }

        private static void GenerateXmlFrom(XmlDocument xmlDocument, BuyerUniversalTransferDocument buyerDataContract)
        {
            var documentNode = xmlDocument.CreateElement("Документ"); // Информация покупателя
            documentNode.SetAttribute("КНД", buyerDataContract.TaxDocumentCode);

            var sellerDocumentInfo = buyerDataContract.SellerDocumentInfo;
            if (!string.IsNullOrEmpty(sellerDocumentInfo.DocumentUid))
                documentNode.SetAttribute("УИД", sellerDocumentInfo.DocumentUid);

            documentNode.SetAttribute("ДатаИнфПок", buyerDataContract.DateCreation.ToString("dd.MM.yyyy")); // Дата формирования файла обмена информации покупателя
            documentNode.SetAttribute("ВремИнфПок", buyerDataContract.DateCreation.ToString("HH.mm.ss")); // Время формирования файла обмена информации покупателя
            documentNode.SetAttribute("НаимЭконСубСост", GetEconomicEntityName(buyerDataContract.DocumentCreator)); // Наименование экономического субъекта - составителя файла обмена информации покупателя

            xmlDocument.DocumentElement.AppendChild(documentNode);

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

            if (buyerDataContract.DocumentCreatorBase is EDM.Models.V5_03.Document creatorBase)
                AddDocumentInfo(xmlDocument, documentNode, [creatorBase], "ОснДоверОргСост"); // Основание, по которому экономический субъект является составителем файла обмена счета-фактуры (информации продавца)
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

            var circumPublicProcNode = xmlDocument.CreateElement("ИнфПокЗаГоскКазн");

            if (!string.IsNullOrEmpty(buyerInfoCircum.PurchasingIdentificationCode))
                circumPublicProcNode.SetAttribute("ИдКодЗак", buyerInfoCircum.PurchasingIdentificationCode);

            circumPublicProcNode.SetAttribute("ЛицСчетПок", buyerInfoCircum.PersonalAccountBuyer);
            circumPublicProcNode.SetAttribute("НаимФинОргПок", buyerInfoCircum.BuyerFinancialAuthorityName);
            circumPublicProcNode.SetAttribute("НомРеестрЗапПок", buyerInfoCircum.BuyerBudgetRegisterNumber);

            if (!string.IsNullOrEmpty(buyerInfoCircum.BuyerBudgetObligationAccountNumber))
                circumPublicProcNode.SetAttribute("УчНомБюдОбязПок", buyerInfoCircum.BuyerBudgetObligationAccountNumber);

            if (!string.IsNullOrEmpty(buyerInfoCircum.BuyerTreasuryCode))
                circumPublicProcNode.SetAttribute("КодКазначПок", buyerInfoCircum.BuyerTreasuryCode);

            if (!string.IsNullOrEmpty(buyerInfoCircum.BuyerTreasuryName))
                circumPublicProcNode.SetAttribute("НаимКазначПок", buyerInfoCircum.BuyerTreasuryName);

            circumPublicProcNode.SetAttribute("ОКТМОПок", buyerInfoCircum.BuyerMunicipalCode);

            if (!string.IsNullOrEmpty(buyerInfoCircum.DeliveryMunicipalCode))
                circumPublicProcNode.SetAttribute("ОКТМОМесПост", buyerInfoCircum.DeliveryMunicipalCode);

            if (buyerInfoCircum.PaymentDate != null)
                circumPublicProcNode.SetAttribute("ДатаОплПред", buyerInfoCircum.PaymentDate.Value.ToString("dd.MM.yyyy"));

            if (!string.IsNullOrEmpty(buyerInfoCircum.FinancialObligationAccountNumber))
                circumPublicProcNode.SetAttribute("УчНомДенОбяз", buyerInfoCircum.FinancialObligationAccountNumber);

            if (!string.IsNullOrEmpty(buyerInfoCircum.PaymentOrder))
                circumPublicProcNode.SetAttribute("ОчерПлат", buyerInfoCircum.PaymentOrder);

            if (buyerInfoCircum.PaymentType != EDM.Models.V5_03.Buyer.Reference.PaymentType.NotSpecified)
                circumPublicProcNode.SetAttribute("ВидПлат", ((int)buyerInfoCircum.PaymentType).ToString());

            foreach (var obligationInfo in buyerInfoCircum.FinancialObligationInfoList)
            {
                var circumPublicProcInfoNode = xmlDocument.CreateElement("ИнфСведДенОбяз");
                circumPublicProcInfoNode.SetAttribute("НомСтр", obligationInfo.RowNumber.ToString());

                if (!string.IsNullOrEmpty(obligationInfo.ObjectFAIPCode))
                    circumPublicProcInfoNode.SetAttribute("КодОбъектФАИП", obligationInfo.ObjectFAIPCode);

                if (obligationInfo.FundType != EDM.Models.Buyer.Reference.FundType.NotSpecified)
                    circumPublicProcInfoNode.SetAttribute("ВидСредств", ((int)obligationInfo.FundType).ToString());

                circumPublicProcInfoNode.SetAttribute("КодПокБюджКласс", obligationInfo.BuyerBudgetClassCode);

                if (!string.IsNullOrEmpty(obligationInfo.BuyerTargetCode))
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
            documentInfoNode.SetAttribute("ПорНомДокИнфПр", sellerDocumentInfo.DocumentNumber); // Порядковый номер (строка 1 счета-фактуры) документа об отгрузке товаров (выполнении работ), передаче имущественных прав (документа об оказании услуг). Указывается (повторяет) значение <НомерДок>, указанное в файле обмена счета-фактуры (информации продавца) или файле обмена информации продавца
            documentInfoNode.SetAttribute("ДатаДокИнфПр", sellerDocumentInfo.DocumentDate); // Дата документа об отгрузке товаров (выполнении работ), передаче имущественных прав (документа об оказании услуг). Указывается (повторяет) значение <ДатаДок>, указанное в файле обмена счета-фактуры (информации продавца) или файле обмена информации продавца

            if (!string.IsNullOrEmpty(dataContract.OperationType))
                documentInfoNode.SetAttribute("ВидОперации", dataContract.OperationType); // Дополнительная информация, позволяющая в автоматизированном режиме определять необходимый для конкретного случая порядок использования информации документа у покупателя

            parentElement.AppendChild(documentInfoNode);

            var confirmNode = xmlDocument.CreateElement("СвПрин"); // Сведения о принятии товаров (результатов выполненных работ), имущественных прав (о подтверждении факта оказания услуг)
            var acceptanceInfo = dataContract.AcceptanceInfo;

            if (!string.IsNullOrEmpty(acceptanceInfo.OperationName))
                confirmNode.SetAttribute("СодОпер", acceptanceInfo.OperationName); // Содержание операции (текст)

            confirmNode.SetAttribute("ДатаПрин", acceptanceInfo.Date.ToString("dd.MM.yyyy")); // Дата принятия товаров (результатов выполненных работ), имущественных прав (подтверждения факта оказания услуг)

            documentInfoNode.AppendChild(confirmNode);

            if (acceptanceInfo.OperationNameInfo is OperationNameInfo operationNameInfo)
            {
                var confirmInfoNode = xmlDocument.CreateElement("КодСодОпер"); // Код содержания операции
                confirmInfoNode.SetAttribute("КодИтога", ((int)operationNameInfo.Code).ToString()); // Код, обозначающий итог приемки товара (работ, услуг, прав).

                if (operationNameInfo.DiscrepancyDocument is EDM.Models.V5_03.Document document)
                {
                    AddDocumentInfo(xmlDocument, confirmNode, [document], "РеквДокРасх");
                }

                confirmNode.AppendChild(confirmInfoNode);
            }

            if (acceptanceInfo.ReceiverPerson is ReceiverPerson receiver)
            {
                var receiverNode = xmlDocument.CreateElement("СвЛицПрин"); // Сведения о лице, принявшем товары (груз)
                
                if (receiver.Employee is ReceiverEmployee receiverEmployee)
                {
                    var receiverEmployeeNode = xmlDocument.CreateElement("РабОргПок"); // Работник организации продавца
                    receiverEmployeeNode.SetAttribute("Должность", receiverEmployee.JobTitle); // Должность

                    if (!string.IsNullOrEmpty(receiverEmployee.OtherInfo))
                        receiverEmployeeNode.SetAttribute("ИныеСвед", receiverEmployee.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                    AddPersonName(xmlDocument, receiverEmployee, receiverEmployeeNode);

                    receiverNode.AppendChild(receiverEmployeeNode);
                }
                else if (receiver.OtherIssuer is OtherIssuer receiverOtherIssue)
                {
                    var receiverOtherNode = xmlDocument.CreateElement("ИнЛицо"); // Сведения о лице, передавшем товар (груз)

                    if (receiverOtherIssue.OrganizationPerson is TransferOrganizationPerson organizationPerson)
                    {
                        var receiverOtherIssueEmployeeNode = xmlDocument.CreateElement("ПредОргПрин"); // Представитель организации, которой доверено принятие товаров (груза)
                        receiverOtherIssueEmployeeNode.SetAttribute("Должность", organizationPerson.JobTitle); // Должность

                        if (!string.IsNullOrEmpty(organizationPerson.OtherInfo))
                            receiverOtherIssueEmployeeNode.SetAttribute("ИныеСвед", organizationPerson.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                        receiverOtherIssueEmployeeNode.SetAttribute("НаимОргПрин", organizationPerson.OrganizationName); // Наименование организации

                        if (!string.IsNullOrEmpty(organizationPerson.OrganizationInn))
                            receiverOtherIssueEmployeeNode.SetAttribute("ИННОргПрин", organizationPerson.OrganizationInn);

                        if (organizationPerson.OrganizationBase is EDM.Models.V5_03.Document orgDocument)
                            AddDocumentInfo(xmlDocument, receiverOtherIssueEmployeeNode, [orgDocument], "ОснДоверОргПрин"); // Основание, по которому организации доверено принятие товаров

                        if (organizationPerson.EmployeeBase is EDM.Models.V5_03.Document empDocument)
                            AddDocumentInfo(xmlDocument, receiverOtherIssueEmployeeNode, [empDocument], "ОснПолнПредПрин"); // Основание полномочий представителя организации на принятие товаров

                        AddPersonName(xmlDocument, organizationPerson, receiverOtherIssueEmployeeNode);

                        receiverOtherNode.AppendChild(receiverOtherIssueEmployeeNode);
                    }
                    else if (receiverOtherIssue.PhysicalPerson is TransferPhysicalPerson physicalPerson)
                    {
                        var receiverOtherIssuePersonNode = xmlDocument.CreateElement("ФЛПрин"); // Представитель организации, которой доверена отгрузка товаров

                        if (!string.IsNullOrEmpty(physicalPerson.PersonInn))
                            receiverOtherIssuePersonNode.SetAttribute("ИННФЛПрин", physicalPerson.PersonInn); // ИНН физического лица, в том числе индивидуального предпринимателя, которому доверен прием

                        if (!string.IsNullOrEmpty(physicalPerson.OtherInfo))
                            receiverOtherIssuePersonNode.SetAttribute("ИныеСвед", physicalPerson.OtherInfo); // Иные сведения, идентифицирующие физическое лицо

                        if (physicalPerson.PersonBase is EDM.Models.V5_03.Document persDocument)
                            AddDocumentInfo(xmlDocument, receiverOtherIssuePersonNode, [persDocument], "ОснДоверФЛ"); // Основание, по которому физическому лицу доверено принятие товаров

                        AddPersonName(xmlDocument, physicalPerson, receiverOtherIssuePersonNode);

                        receiverOtherNode.AppendChild(receiverOtherIssuePersonNode);

                    }
                    else
                    {
                        throw new ArgumentNullException("Не указаны сведения о ином лице, принявшем товар (ИнЛицо).");
                    }

                    receiverNode.AppendChild(receiverOtherNode);
                }
                else
                {
                    throw new ArgumentNullException("Не указаны сведения о лице, принявшем товар (СвЛицПрин).");
                }

                confirmNode.AppendChild(receiverNode);

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
    }
}
