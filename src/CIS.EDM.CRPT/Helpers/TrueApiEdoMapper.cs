using System;
using System.Collections.Generic;
using System.Linq;
using CIS.EDM.CRPT.Models;
using CIS.EDM.Models;
using CIS.EDM.Models.Reference;
using Spoleto.TrueApi.Edo.Enums;
using Spoleto.TrueApi.Edo.Models;
using V = CIS.EDM.Models.V5_03;
using VS = CIS.EDM.Models.V5_03.Seller;
using VA = CIS.EDM.Models.V5_03.Seller.Address;
using VB = CIS.EDM.Models.V5_03.Buyer;
using VBR = CIS.EDM.Models.V5_03.Buyer.Reference;
using E = Spoleto.TrueApi.Edo.Models.Upd;

namespace CIS.EDM.CRPT.Helpers
{
    /// <summary>
    /// Перевод моделей CIS.EDM в модели Spoleto.TrueApi.Edo и обратно для <see cref="Providers.TrueApiCRPTProvider"/>.
    /// </summary>
    /// <remarks>
    /// Перечисления с одинаковыми кодами переводятся по числовому значению, ставка налога - по имени.
    /// Даты переносятся как есть: Spoleto.TrueApi.Edo читает и пишет их так же, как конвертеры CIS.EDM.
    /// </remarks>
    internal static class TrueApiEdoMapper
    {
        #region Списки документов

        /// <summary>
        /// Критерии отбора для запросов True API. В True API статус и тип - одно значение, а в CIS.EDM - списки,
        /// поэтому на каждое сочетание статуса и типа - свой запрос.
        /// </summary>
        /// <returns>Критерии каждого запроса. Без критериев - один запрос без них.</returns>
        public static List<EdoDocumentSearchModel> MapSearchModels(DocumentCollectionSearchModel x)
        {
            if (x == null)
                return [null];

            var searchModel = new EdoDocumentSearchModel
            {
                Limit = x.Limit,
                Offset = x.Offset,
                CreatedFrom = x.CreatedFrom,
                CreatedTo = x.CreatedTo,
                PartnerInn = x.PartnerInn,
                SortBy = x.SortBy switch
                {
                    null => null,
                    SortField.created_at => EdoDocumentSortField.CreatedAt,
                    _ => throw new NotSupportedException($"Сортировка по {x.SortBy} не поддерживается: True API сортирует документы только по времени создания ({SortField.created_at}).")
                },
                Asc = x.Asc
            };

            List<EdoDocumentStatus?> statuses = x.Status is { Count: > 0 } ? x.Status.Distinct().Select(s => (EdoDocumentStatus?)(EdoDocumentStatus)(int)s).ToList() : [null];
            List<EdoDocumentType?> types = x.Type is { Count: > 0 } ? x.Type.Distinct().Select(t => (EdoDocumentType?)(EdoDocumentType)(int)t).ToList() : [null];

            return statuses.SelectMany(s => types.Select(t => searchModel with { Status = s, Type = t })).ToList();
        }

        /// <summary>
        /// Ответы на запросы из <see cref="MapSearchModels"/> одним списком.
        /// </summary>
        /// <remarks>
        /// Ответ на один запрос остаётся в порядке сервиса. Ответы на несколько запросов объединяются и сортируются заново по времени создания:
        /// каждый отсортирован сервисом сам по себе. Каждый запрос получает свою страницу из <see cref="DocumentCollectionSearchModel.Limit"/> документов,
        /// поэтому объединённая страница бывает длиннее. Следующая страница есть, если она есть хотя бы у одного запроса.
        /// </remarks>
        public static DocumentCollection Map(IReadOnlyList<EdoDocumentCollection> collections, bool asc)
        {
            if (collections.Count == 1)
                return Map(collections[0]);

            var groups = collections.SelectMany(x => x.Items).DistinctBy(x => x.Id);
            groups = asc ? groups.OrderBy(x => x.CreatedAt) : groups.OrderByDescending(x => x.CreatedAt);

            return new DocumentCollection
            {
                Items = groups.Select(Map).ToList(),
                HasNextPage = collections.Any(x => x.HasNextPage)
            };
        }

        public static DocumentCollection Map(EdoDocumentCollection x) => new()
        {
            Items = x.Items.Select(Map).ToList(),
            HasNextPage = x.HasNextPage
        };

        private static Document Map(EdoDocumentGroup x) => new()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt ?? default,
            Date = x.Date ?? default,
            Documents = x.Documents.Select(Map).ToList(),
            GroupId = x.GroupId,
            Number = x.Number,
            // Контрагент: у входящих True API отдаёт отправителя, у исходящих - получателя.
            Sender = Map(x.Sender ?? x.Recipient),
            Status = (DocumentState)(int)x.Status,
            TotalPrice = x.TotalPrice ?? 0,
            TotalVatAmount = x.TotalVatAmount ?? 0,
            Type = (DocumentType)(int)x.Type,
            CreateTimestamp = x.CreateTimestamp ?? default,
            ExportTimestamp = x.ExportTimestamp ?? default
        };

        private static DocumentInfo Map(EdoDocumentInfo x) => new()
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt ?? default,
            Date = x.Date ?? default,
            Number = x.Number,
            ProcessedAt = x.ProcessedAt ?? default,
            Status = (DocumentState)(int)x.Status,
            TotalPrice = x.TotalPrice ?? 0,
            TotalVatAmount = x.TotalVatAmount ?? 0,
            Type = (DocumentType)(int)x.Type,
            IsTitleSigned = x.IsTitleSigned
        };

        private static FirmInfo Map(EdoParticipant x) => x == null ? null : new()
        {
            Id = x.Id ?? 0,
            ExtraId = x.ExtraId,
            Inn = x.Inn,
            Kpp = x.Kpp,
            Ogrn = x.Ogrn,
            // У ИП наименования может не быть, только ФИО.
            Name = x.Name ?? x.Fio?.ToString()
        };

        #endregion

        #region Файлы документов

        public static SignedDocumentInfo Map(EdoSignedDocumentInfo x) => new()
        {
            Content = x.Content,
            DetachedSignature = Convert.ToBase64String(x.Signature)
        };

        public static SellerDocumentInfo Map(EdoSellerDocumentInfo x) => new()
        {
            FileId = x.FileId,
            DateCreation = x.DateCreation,
            TimeCreation = x.TimeCreation,
            // Как в XmlParserV5_03: нет атрибута - пустая строка.
            DocumentName = x.DocumentName ?? string.Empty,
            Function = x.Function switch
            {
                EdoDocumentFunction.Invoice => UniversalTransferDocumentFunction.СЧФ,
                EdoDocumentFunction.InvoiceAndPrimaryDocument => UniversalTransferDocumentFunction.СЧФДОП,
                EdoDocumentFunction.PrimaryDocument => UniversalTransferDocumentFunction.ДОП,
                EdoDocumentFunction.CommissionSale => UniversalTransferDocumentFunction.СвРК,
                EdoDocumentFunction.CommissionPurchase => UniversalTransferDocumentFunction.СвЗК,
                _ => throw new ArgumentOutOfRangeException(nameof(x), x.Function, "Неизвестная функция документа.")
            },
            DocumentNumber = x.DocumentNumber ?? string.Empty,
            DocumentDate = x.DocumentDate,
            DocumentUid = x.DocumentUid ?? string.Empty,
            IsUPDi = x.IsUPDi,
            EdsBodyList = [Convert.ToBase64String(x.Signature)]
        };

        #endregion

        #region УПД формата 970

        /// <summary>
        /// Информация продавца.
        /// </summary>
        /// <remarks>
        /// Отправитель и получатель - <see cref="EdmParticipant.FullId"/>. Сведений о прослеживаемости в новой модели нет: «ЭДО Лайт» их не принимает.
        /// </remarks>
        public static E.EdoSellerUniversalTransferDocument Map(VS.SellerUniversalTransferDocument d) => new()
        {
            FileId = d.FileId,
            ApplicationCreator = d.ApplicationCreator,
            SenderEdoId = d.SenderEdmParticipant.FullId,
            RecipientEdoId = d.RecipientEdmParticipant.FullId,
            DateCreation = d.DateCreation,
            Function = d.Function switch
            {
                UniversalTransferDocumentFunction.СЧФ => EdoDocumentFunction.Invoice,
                UniversalTransferDocumentFunction.СЧФДОП => EdoDocumentFunction.InvoiceAndPrimaryDocument,
                UniversalTransferDocumentFunction.ДОП => EdoDocumentFunction.PrimaryDocument,
                UniversalTransferDocumentFunction.СвРК => EdoDocumentFunction.CommissionSale,
                UniversalTransferDocumentFunction.СвЗК => EdoDocumentFunction.CommissionPurchase,
                _ => throw new ArgumentOutOfRangeException(nameof(d), d.Function, "Неизвестная функция документа.")
            },
            DocumentNumber = d.DocumentNumber,
            DocumentDate = d.DocumentDate,
            DocumentUid = d.DocumentUid,
            DocumentName = d.DocumentName,
            DocumentEconomicName = d.DocumentEconomicName,
            ApprovedStructureAdditionalInfoFields = d.ApprovedStructureAdditionalInfoFields,
            DocumentCreator = Map(d.DocumentCreator),
            DocumentCreatorBase = Map(d.DocumentCreatorBase),
            RevisionNumber = d.RevisionNumber,
            RevisionDate = d.RevisionDate,
            CorrectedSellerFileName = d.CorrectedSellerFileName,
            CorrectedBuyerFileName = d.CorrectedBuyerFileName,
            Sellers = d.Sellers.Select(Map).ToList(),
            Shippers = d.Shippers?.Select(Map).ToList(),
            Consignees = d.Consignees?.Select(Map).ToList(),
            PaymentDocumentInfoList = d.PaymentDocumentInfoList?.Select(x => new E.EdoPaymentDocumentInfo { Number = x.Number, Date = x.Date, Total = x.Total }).ToList(),
            DocumentShipmentList = d.DocumentShipmentList?.Select(Map).ToList(),
            Buyers = d.Buyers.Select(Map).ToList(),
            CurrencyCode = d.CurrencyCode,
            CurrencyName = d.CurrencyName,
            CurrencyRate = d.CurrencyRate,
            AdditionalTransactionParticipantInfo = Map(d.AdditionalTransactionParticipantInfo),
            OtherEconomicInfo = Map(d.OtherEconomicInfo),
            Items = d.Items.Select(Map).ToList(),
            TransferInfo = d.TransferInfo == null ? null : new E.EdoTransferInfo
            {
                TransferDetails = Map(d.TransferInfo.TransferDetails),
                OtherEconomicInfo = Map(d.TransferInfo.OtherEconomicInfo)
            },
            Signers = d.Signers.Select(Map).ToList()
        };

        /// <summary>
        /// Информация покупателя.
        /// </summary>
        /// <remarks>
        /// Сведения о файле продавца, отправителя и получателя <see cref="Spoleto.TrueApi.Edo.Providers.TrueApiEdoProvider"/> берёт из входящего документа,
        /// поэтому SellerDocumentInfo, SenderEdmParticipant и RecipientEdmParticipant не переносятся.
        /// FileId тоже: геттер CIS.EDM формирует его без файла продавца, с нулевыми признаками N2-N6.
        /// </remarks>
        public static E.EdoBuyerUniversalTransferDocument Map(VB.BuyerUniversalTransferDocument d) => new()
        {
            DocumentId = d.EdmDocumentId,
            ApplicationCreator = d.ApplicationCreator,
            DateCreation = d.DateCreation,
            DocumentCreator = Map(d.DocumentCreator),
            DocumentCreatorBase = Map(d.DocumentCreatorBase),
            OperationType = d.OperationType,
            AcceptanceInfo = new()
            {
                OperationName = d.AcceptanceInfo.OperationName,
                Date = d.AcceptanceInfo.Date,
                // Без КодСодОпер старый генератор формировал файл не по формату, новая модель берёт итог «принято без расхождений».
                OperationNameInfo = d.AcceptanceInfo.OperationNameInfo is { } o
                    ? new() { Code = (EdoOperationNameCode)(int)o.Code, DiscrepancyDocument = Map(o.DiscrepancyDocument) }
                    : new(),
                ReceiverPerson = d.AcceptanceInfo.ReceiverPerson is { } r ? new()
                {
                    Employee = r.Employee is { } e ? new() { Surname = e.Surname, FirstName = e.FirstName, Patronymic = e.Patronymic, JobTitle = e.JobTitle, OtherInfo = e.OtherInfo } : null,
                    OtherIssuer = r.OtherIssuer is { } oi ? new()
                    {
                        OrganizationPerson = oi.OrganizationPerson is { } op ? new()
                        {
                            Surname = op.Surname, FirstName = op.FirstName, Patronymic = op.Patronymic, JobTitle = op.JobTitle, OtherInfo = op.OtherInfo,
                            OrganizationName = op.OrganizationName, OrganizationInn = op.OrganizationInn, OrganizationBase = Map(op.OrganizationBase), EmployeeBase = Map(op.EmployeeBase)
                        } : null,
                        PhysicalPerson = oi.PhysicalPerson is { } pp ? new()
                        {
                            Surname = pp.Surname, FirstName = pp.FirstName, Patronymic = pp.Patronymic, PersonInn = pp.PersonInn, OtherInfo = pp.OtherInfo, PersonBase = Map(pp.PersonBase)
                        } : null
                    } : null
                } : null
            },
            OtherEconomicInfo = Map(d.OtherEconomicInfo),
            BuyerInfoCircumPublicProc = d.BuyerInfoCircumPublicProc is { } b ? new()
            {
                PurchasingIdentificationCode = b.PurchasingIdentificationCode, PersonalAccountBuyer = b.PersonalAccountBuyer,
                BuyerFinancialAuthorityName = b.BuyerFinancialAuthorityName, BuyerBudgetRegisterNumber = b.BuyerBudgetRegisterNumber,
                BuyerBudgetObligationAccountNumber = b.BuyerBudgetObligationAccountNumber, BuyerTreasuryCode = b.BuyerTreasuryCode, BuyerTreasuryName = b.BuyerTreasuryName,
                BuyerMunicipalCode = b.BuyerMunicipalCode, DeliveryMunicipalCode = b.DeliveryMunicipalCode, PaymentDate = b.PaymentDate,
                FinancialObligationAccountNumber = b.FinancialObligationAccountNumber, PaymentOrder = b.PaymentOrder,
                // NotSpecified - атрибута ВидПлатежа нет.
                PaymentType = b.PaymentType == VBR.PaymentType.NotSpecified ? null : (EdoPaymentType)(int)b.PaymentType,
                // ВидСредств обязателен, NotSpecified в новой модели нет.
                FinancialObligationInfoList = b.FinancialObligationInfoList.Select(f => new E.EdoFinancialObligationInfo
                {
                    RowNumber = f.RowNumber, ObjectFAIPCode = f.ObjectFAIPCode, FundType = (EdoFundType)(int)f.FundType,
                    BuyerBudgetClassCode = f.BuyerBudgetClassCode, BuyerTargetCode = f.BuyerTargetCode, AmountAdvance = f.AmountAdvance
                }).ToList()
            } : null,
            Signers = d.Signers.Select(Map).ToList()
        };

        private static E.EdoOrganization Map(VS.Organization x) => x == null ? null : new()
        {
            Okpo = x.Okpo,
            OpfCode = x.OpfCode,
            OpfName = x.OpfName,
            Department = x.Department,
            OrganizationAdditionalInfo = x.OrganizationAdditionalInfo,
            ShortName = x.ShortName,
            OrganizationIdentificationInfo = new()
            {
                LegalPerson = x.OrganizationIdentificationInfo.LegalPerson is { } l ? new() { Name = l.Name, Inn = l.Inn, Kpp = l.Kpp } : null,
                IndividualEntrepreneur = x.OrganizationIdentificationInfo.IndividualEntrepreneur is { } i ? new()
                {
                    Surname = i.Surname, FirstName = i.FirstName, Patronymic = i.Patronymic, Inn = i.Inn,
                    IndividualEntrepreneurRegistrationCertificate = i.IndividualEntrepreneurRegistrationCertificate,
                    Ogrnip = i.Ogrnip, OgrnipDate = i.OgrnipDate, OtherInfo = i.OtherInfo
                } : null,
                PhysicalPerson = x.OrganizationIdentificationInfo.PhysicalPerson is { } p ? new()
                {
                    Surname = p.Surname, FirstName = p.FirstName, Patronymic = p.Patronymic, Inn = p.Inn, Status = p.Status, OtherInfo = p.OtherInfo
                } : null,
                ForeignPerson = Map(x.OrganizationIdentificationInfo.ForeignPerson)
            },
            Address = x.Address == null ? null : new()
            {
                GlobalLocationNumber = x.Address.GlobalLocationNumber,
                RussianAddress = x.Address.RussianAddress is { } r ? new()
                {
                    ZipCode = r.ZipCode, RegionCode = r.RegionCode, Region = r.Region, Territory = r.Territory, City = r.City,
                    Locality = r.Locality, Street = r.Street, Building = r.Building, Block = r.Block, Apartment = r.Apartment, OtherInfo = r.OtherInfo
                } : null,
                AddressCode = x.Address.AddressCode is { } c ? new()
                {
                    UniqueCode = c.UniqueCode, ZipCode = c.ZipCode, Region = c.Region, RegionName = c.RegionName,
                    MunicipalDistrict = Map(c.MunicipalDistrict), UrbanRuralSettlement = Map(c.UrbanRuralSettlement),
                    Settlement = c.Settlement == null ? null : new() { Type = c.Settlement.Type, Name = c.Settlement.Name },
                    PlanningStructureElement = Map(c.PlanningStructureElement), StreetRoadNetworkElement = Map(c.StreetRoadNetworkElement),
                    LandPlotNumber = c.LandPlotNumber, BuildingList = c.BuildingList?.Select(Map).ToList(),
                    BuildingRoom = Map(c.BuildingRoom), ApartmentRoom = Map(c.ApartmentRoom)
                } : null,
                AddressInformation = x.Address.AddressInformation is { } a ? new() { CountryCode = a.CountryCode, CountryName = a.CountryName, Address = a.Address } : null
            },
            BankAccountDetails = x.BankAccountDetails == null ? null : new()
            {
                BankAccountNumber = x.BankAccountDetails.BankAccountNumber,
                BankDetails = x.BankAccountDetails.BankDetails is { } b ? new() { BankName = b.BankName, BankId = b.BankId, CorrespondentAccount = b.CorrespondentAccount } : null
            },
            Contact = x.Contact == null ? null : new() { PhoneList = x.Contact.PhoneList, EmailList = x.Contact.EmailList, OtherInfo = x.Contact.OtherInfo }
        };

        private static E.EdoAddressElementCode Map(VA.AddressElementCode x) => x == null ? null : new() { TypeCode = x.TypeCode, Name = x.Name };

        private static E.EdoAddressElement Map(VA.AddressElementType x) => x == null ? null : new() { Type = x.Type, Name = x.Name };

        private static E.EdoAddressElementNumber Map(VA.AddressElementNumber x) => x == null ? null : new() { Type = x.Type, Number = x.Number };

        private static E.EdoForeignEntity Map(V.ForeignEntity x) => x == null ? null : new()
        {
            Status = x.Status, CountryCode = x.CountryCode, CountryName = x.CountryName, Name = x.Name, Identifier = x.Identifier, OtherInfo = x.OtherInfo
        };

        private static E.EdoDocumentDetails Map(V.Document x) => x == null ? null : new()
        {
            Name = x.Name, Number = x.Number, Date = x.Date, FileId = x.FileId, DocumentId = x.DocumentId,
            StorageSystemId = x.StorageSystemId, SystemUrl = x.SystemUrl, OtherInfo = x.OtherInfo,
            Creators = x.Creators?.Select(c => new E.EdoDocumentCreator
            {
                InnLegalEntity = c.InnLegalEntity, InnIndividual = c.InnIndividual, ForeignEntity = Map(c.ForeignEntity), AuthorityShortName = c.AuthorityShortName
            }).ToList()
        };

        private static E.EdoOtherEconomicInfo Map(OtherEconomicInfo x) => x == null ? null : new()
        {
            InfoFileId = x.InfoFileId,
            Items = x.Items?.Select(i => new E.EdoOtherEconomicInfoItem { Id = i.Id, Value = i.Value }).ToList()
        };

        private static E.EdoAdditionalTransactionParticipantInfo Map(VS.AdditionalTransactionParticipantInfo x) => x == null ? null : new()
        {
            GovernmentContractInfo = x.GovernmentContractInfo,
            InvoiceFormationType = (EdoInvoiceFormationType)(int)x.InvoiceFormationType,
            UPDInvoiceFormationType = x.UPDInvoiceFormationType,
            UPDFormationType = x.UPDFormationType,
            ObligationInfoList = x.ObligationInfoList?.Select(o => new E.EdoObligationInfo { Code = o.Code, Name = o.Name }).ToList(),
            SellerInfoCircumPublicProc = x.SellerInfoCircumPublicProc is { } s ? new()
            {
                DateStateContract = s.DateStateContract, NumberStateContract = s.NumberStateContract, PersonalAccountSeller = s.PersonalAccountSeller,
                SellerBudgetClassCode = s.SellerBudgetClassCode, SellerTargetCode = s.SellerTargetCode, SellerTreasuryCode = s.SellerTreasuryCode, SellerTreasuryName = s.SellerTreasuryName
            } : null,
            FactorInfo = Map(x.FactorInfo),
            MainAssignMonetaryClaim = Map(x.MainAssignMonetaryClaim),
            SupportingDocumentList = x.SupportingDocumentList?.Select(Map).ToList()
        };

        private static E.EdoInvoiceItem Map(VS.InvoiceItem x) => new()
        {
            ProductName = x.ProductName, UnitCode = x.UnitCode, UnitName = x.UnitName, Quantity = x.Quantity, Price = x.Price,
            // Стоимость с налогом в новой модели обязательна.
            SumWithoutVat = x.SumWithoutVat, TaxRate = Enum.Parse<EdoTaxRate>(x.TaxRate.ToString()), Sum = x.Sum.Value, Excise = x.Excise, Vat = x.Vat,
            CustomsDeclarationList = x.CustomsDeclarationList?.Select(c => new E.EdoCustomsDeclaration { CountryCode = c.CountryCode, DeclarationNumber = c.DeclarationNumber }).ToList(),
            AdditionalInfo = x.AdditionalInfo is { } a ? new()
            {
                Type = (EdoInvoiceItemType)(int)a.Type, AdditionalTypeInfo = a.AdditionalTypeInfo, ItemToRelease = a.ItemToRelease,
                Characteristic = a.Characteristic, Kind = a.Kind, Series = a.Series, Article = a.Article, Code = a.Code,
                GlobalTradeItemNumber = a.GlobalTradeItemNumber, CatalogCode = a.CatalogCode, FeaccCode = a.FeaccCode,
                ProductTypeCode = a.ProductTypeCode, Okdp2ProductCode = a.Okdp2ProductCode, AdditionalOperationInfo = a.AdditionalOperationInfo,
                CountryNames = a.CountryNames, SupportingDocumentList = a.SupportingDocumentList?.Select(Map).ToList(),
                TaxAccountingAmortization = a.TaxAccountingAmortization is { } t ? new()
                {
                    AmortizationGroup = t.AmortizationGroup, OkofCode = t.OkofCode, EstablishedUsefulLife = t.EstablishedUsefulLife, ActualUsefulLifeMonths = t.ActualUsefulLifeMonths
                } : null,
                RecoveredTaxAmount = a.RecoveredTaxAmount is { } r ? new() { Amount = r.Amount, WithoutVat = r.WithoutVat } : null,
                ItemIdentificationNumberList = a.ItemIdentificationNumberList?.Select(n => new E.EdoInvoiceItemIdentificationNumber
                {
                    PackageId = n.PackageId, MarkedProductQuantity = n.MarkedProductQuantity, ProductionBatchCode = n.ProductionBatchCode,
                    MarkItems = n.MarkItems, SecondaryPackageItems = n.SecondaryPackageItems
                }).ToList(),
                GovernmentSystemAdditionalInfoList = a.GovernmentSystemAdditionalInfoList?.Select(g => new E.EdoGovernmentSystemAdditionalInfo
                {
                    InformationSystemName = g.InformationSystemName, AccountingUnit = g.AccountingUnit, OtherInfo = g.OtherInfo, AccountingUnitIds = g.AccountingUnitIds
                }).ToList()
            } : null,
            OtherEconomicInfoItemList = x.OtherEconomicInfoItemList?.Select(i => new E.EdoOtherEconomicInfoItem { Id = i.Id, Value = i.Value }).ToList()
        };

        private static E.EdoTransferDetails Map(VS.TransferDetails x) => new()
        {
            OperationName = x.OperationName, OperationType = x.OperationType, Date = x.Date, StartDate = x.StartDate, EndDate = x.EndDate,
            TransferDocuments = x.TransferDocuments?.Select(Map).ToList(),
            SenderPerson = x.SenderPerson == null ? null : new()
            {
                Employee = x.SenderPerson.Employee is { } e ? new() { Surname = e.Surname, FirstName = e.FirstName, Patronymic = e.Patronymic, JobTitle = e.JobTitle, OtherInfo = e.OtherInfo } : null,
                OtherIssuer = x.SenderPerson.OtherIssuer is { } o ? new()
                {
                    OrganizationPerson = o.OrganizationPerson is { } op ? new()
                    {
                        Surname = op.Surname, FirstName = op.FirstName, Patronymic = op.Patronymic, JobTitle = op.JobTitle, OtherInfo = op.OtherInfo,
                        OrganizationName = op.OrganizationName, OrganizationInn = op.OrganizationInn, OrganizationBase = Map(op.OrganizationBase), EmployeeBase = Map(op.EmployeeBase)
                    } : null,
                    PhysicalPerson = o.PhysicalPerson is { } pp ? new()
                    {
                        // В CIS.EDM свойство называется Othernfo.
                        Surname = pp.Surname, FirstName = pp.FirstName, Patronymic = pp.Patronymic, PersonInn = pp.PersonInn, OtherInfo = pp.Othernfo, PersonBase = Map(pp.PersonBase)
                    } : null
                } : null
            },
            Transportation = x.Transportation is { } t ? new() { TransferTextInfo = t.TransferTextInfo, Incoterms = t.Incoterms, IncotermsVersion = t.IncotermsVersion } : null,
            CreatedThingInfo = x.CreatedThingInfo is { } c ? new() { Date = c.Date, Information = c.Information, Document = Map(c.Document) } : null
        };

        private static E.EdoSigner Map(V.SignerBase x) => new()
        {
            JobTitle = x.JobTitle,
            SignatureType = x.SignatureType == null ? null : (EdoSignatureType)(int)x.SignatureType.Value,
            SigningDate = x.SigningDate,
            AuthorityConfirmationType = (EdoAuthorityConfirmationType)(int)x.AuthorityConfirmationType,
            AdditionalInfo = x.AdditionalInfo,
            Person = new() { Surname = x.Person.Surname, FirstName = x.Person.FirstName, Patronymic = x.Person.Patronymic },
            ElectronicPoAInfo = x.ElectronicPoAInfo is { } e ? new()
            {
                RegistrationNumber = e.RegistrationNumber, IssueDate = e.IssueDate, InternalNumber = e.InternalNumber,
                InternalRegistrationDate = e.InternalRegistrationDate, SystemIdentification = e.SystemIdentification, SystemUrl = e.SystemURL
            } : null,
            PaperPoAInfo = x.PaperPoAInfo is { } p ? new()
            {
                IssueDate = p.IssueDate, InternalNumber = p.InternalNumber, TrusteeInfo = p.TrusteeInfo,
                Signer = p.Signer == null ? null : new() { Surname = p.Signer.Surname, FirstName = p.Signer.FirstName, Patronymic = p.Signer.Patronymic }
            } : null
        };

        #endregion
    }
}
