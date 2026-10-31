using CIS.EDM.Models;
using CIS.EDM.Models.Reference;
using V = CIS.EDM.Models.V5_03;
using VS = CIS.EDM.Models.V5_03.Seller;
using VA = CIS.EDM.Models.V5_03.Seller.Address;
using VB = CIS.EDM.Models.V5_03.Buyer;
using VR = CIS.EDM.Models.V5_03.Reference;
using VSR = CIS.EDM.Models.V5_03.Seller.Reference;
using VBR = CIS.EDM.Models.V5_03.Buyer.Reference;

namespace CIS.EDM.CRPT.Tests
{
    /// <summary>
    /// Модели УПД формата 970 для сверки XML <see cref="Providers.TrueApiCRPTProvider"/> и <see cref="Providers.CRPTProvider"/>: вместе они проходят все ветки маппинга.
    /// </summary>
    internal static class UniversalTransferDocumentSamples
    {
        private static VB.BuyerUniversalTransferDocument Buyer(string number, VS.Organization creator) => new()
        {
            EdmDocumentId = "220262cf-0fe7-4d3f-996a-038181ba7812",
            FileId = $"ON_NSCHFDOPPOK_2LT-2_2LT-1_20260403_00000000-0000-0000-0000-00000000000{number}_0_1_0_0_0_00",
            SenderEdmParticipant = new EdmParticipant("2LT", "1"),
            RecipientEdmParticipant = new EdmParticipant("2LT", "2"),
            DocumentCreator = creator,
            AcceptanceInfo = new() { Date = new DateTime(2026, 4, 3) },
            Signers = [new() { JobTitle = "Директор", AuthorityConfirmationType = VR.AuthorityConfirmationType.ElectronicSignatureData, Person = new() { Surname = "Петров", FirstName = "Петр", Patronymic = "Петрович" } }]
        };

        public static VB.BuyerUniversalTransferDocument BuyerFull()
        {
            var document = Buyer("1", Legal("ООО «Покупатель»", "0000000000", "000001001"));
            document.DocumentCreatorBase = Doc("Доверенность", "Д-5") with { Creators = [new() { InnLegalEntity = "0000000000" }] };
            document.AcceptanceInfo = new()
            {
                OperationName = "Товары принял без претензий", Date = new DateTime(2026, 4, 3),
                ReceiverPerson = new() { Employee = new() { Surname = "Сидоров", FirstName = "Сидор", Patronymic = "Сидорович", JobTitle = "Кладовщик", OtherInfo = "иное" } }
            };
            document.OtherEconomicInfo = new VB.OtherEconomicInfo4 { InfoFileId = "44444444-0000-0000-0000-000000000000", Items = [new() { Id = "МЧД", Value = "444f44d4" }, new() { Id = "Ключ" }] };
            document.BuyerInfoCircumPublicProc = new()
            {
                PurchasingIdentificationCode = "261770000000077010100100010000000244", PersonalAccountBuyer = "03731000001", BuyerFinancialAuthorityName = "Министерство финансов Российской Федерации",
                BuyerBudgetRegisterNumber = "00100001", BuyerBudgetObligationAccountNumber = "0373100000126000001", BuyerTreasuryCode = "7300", BuyerTreasuryName = "УФК по г. Москве",
                BuyerMunicipalCode = "45382000", DeliveryMunicipalCode = "45382000", PaymentDate = new DateTime(2026, 4, 30), FinancialObligationAccountNumber = "0373100000126000002",
                PaymentOrder = "5", PaymentType = VBR.PaymentType.Urgent,
                FinancialObligationInfoList =
                [
                    new() { RowNumber = 1, ObjectFAIPCode = "ФАИП-1", FundType = CIS.EDM.Models.Buyer.Reference.FundType.Budget, BuyerBudgetClassCode = "00000000000000000244", BuyerTargetCode = "Ц-1", AmountAdvance = 10.5M },
                    new() { RowNumber = 2, FundType = CIS.EDM.Models.Buyer.Reference.FundType.LegalEntity, BuyerBudgetClassCode = "00000000000000000000" }
                ]
            };
            document.Signers =
            [
                new() { JobTitle = "Директор", SignatureType = VR.SignatureType.QualifiedElectronicSignature, SigningDate = new DateTime(2026, 4, 3), AuthorityConfirmationType = VR.AuthorityConfirmationType.SystemMachineReadablePoAWithDocInfo, AdditionalInfo = "доп", Person = new() { Surname = "Петров", FirstName = "Петр", Patronymic = "Петрович" },
                    ElectronicPoAInfo = new() { RegistrationNumber = Guid.Parse("222f22d2-2222-222a-2ff2-fa22e22afc22"), IssueDate = new DateTime(2026, 1, 1), InternalNumber = "2", InternalRegistrationDate = new DateTime(2026, 1, 2), SystemIdentification = "https://m4d.nalog.gov.ru/", SystemURL = "https://m4d.nalog.gov.ru/" } }
            ];

            return document;
        }

        public static VB.BuyerUniversalTransferDocument BuyerOrganizationPerson()
        {
            var ie = new VS.Organization { OrganizationIdentificationInfo = new() { IndividualEntrepreneur = new() { Surname = "Иванов", FirstName = "Иван", Patronymic = "Иванович", Inn = "500100732259" } }, Address = Moscow() };
            var document = Buyer("2", ie);
            document.AcceptanceInfo = new()
            {
                OperationName = "Товары приняты с расхождениями", Date = new DateTime(2026, 4, 3),
                OperationNameInfo = new() { Code = VBR.OperationNameCode.AcceptedWithDiscrepancies },
                ReceiverPerson = new()
                {
                    OtherIssuer = new()
                    {
                        OrganizationPerson = new() { Surname = "Орлов", FirstName = "Олег", JobTitle = "Водитель", OtherInfo = "иное", OrganizationName = "ООО Перевозчик", OrganizationInn = "7700000002", OrganizationBase = Doc("Договор перевозки", "П-1"), EmployeeBase = Doc("Доверенность", "Д-2") with { Creators = [new() { InnIndividual = "500100732259" }] } }
                    }
                }
            };
            document.Signers =
            [
                new() { JobTitle = "Индивидуальный предприниматель", AuthorityConfirmationType = VR.AuthorityConfirmationType.PaperBasedDocument, Person = new() { Surname = "Иванов", FirstName = "Иван", Patronymic = "Иванович" },
                    PaperPoAInfo = new() { IssueDate = new DateTime(2026, 2, 1), InternalNumber = "б/н", TrusteeInfo = "ИП", Signer = new() { Surname = "Петров", FirstName = "Петр" } } },
                new() { JobTitle = "Бухгалтер", AuthorityConfirmationType = VR.AuthorityConfirmationType.ElectronicSignatureData, Person = new() { Surname = "Иванова", FirstName = "Мария" } }
            ];

            return document;
        }

        public static VB.BuyerUniversalTransferDocument BuyerPhysicalPerson()
        {
            var physical = new VS.Organization { OrganizationIdentificationInfo = new() { PhysicalPerson = new() { Surname = "Кузнецов", FirstName = "Кузьма", Patronymic = "Кузьмич", Inn = "500100732260" } } };
            var document = Buyer("3", physical);
            document.AcceptanceInfo = new()
            {
                Date = new DateTime(2026, 4, 4),
                OperationNameInfo = new() { Code = VBR.OperationNameCode.NotAccepted },
                ReceiverPerson = new() { OtherIssuer = new() { PhysicalPerson = new() { Surname = "Смирнов", FirstName = "Семен", PersonInn = "500100732261", OtherInfo = "паспорт", PersonBase = Doc("Доверенность", "Д-3") } } }
            };

            return document;
        }

        public static VB.BuyerUniversalTransferDocument BuyerKnownDifferences()
        {
            var ie = new VS.Organization { OrganizationIdentificationInfo = new() { IndividualEntrepreneur = new() { Surname = "Иванов", FirstName = "Иван", Inn = "500100732259" } }, Address = Moscow() };
            var document = Buyer("4", ie);
            document.OperationType = "Поставка";
            document.AcceptanceInfo = new()
            {
                Date = new DateTime(2026, 4, 3),
                OperationNameInfo = new() { Code = VBR.OperationNameCode.AcceptedWithDiscrepancies, DiscrepancyDocument = Doc("Акт о расхождениях", "Р-1") with { FileId = "ON_AKTRAZ_1" } }
            };
            document.Signers = [new() { AuthorityConfirmationType = VR.AuthorityConfirmationType.ElectronicSignatureData, Person = new() { Surname = "Иванов", FirstName = "Иван" } }];

            return document;
        }

        private static VS.Organization Legal(string name, string inn, string kpp) => new()
        {
            OrganizationIdentificationInfo = new() { LegalPerson = new() { Name = name, Inn = inn, Kpp = kpp } },
            Address = Moscow()
        };

        private static VA.Address Moscow() => new() { RussianAddress = new() { RegionCode = "77", Region = "г. Москва" } };

        private static V.Document Doc(string name, string number) => new() { Name = name, Number = number, Date = new DateTime(2026, 4, 1) };

        public static VS.SellerUniversalTransferDocument Full()
        {
            var seller = Legal("ООО «Продавец»", "7777777777", "777701001") with
            {
                Okpo = "12345678", OpfCode = "12300", OpfName = "Общество с ограниченной ответственностью", Department = "Склад № 1",
                OrganizationAdditionalInfo = "Для участника", ShortName = "Продавец",
                Address = new() { GlobalLocationNumber = "4600000000001", RussianAddress = new() { ZipCode = "101000", RegionCode = "77", Region = "г. Москва", Territory = "р-н", City = "Москва", Locality = "пос.", Street = "ул. Тверская", Building = "1", Block = "2", Apartment = "3", OtherInfo = "офис 4" } },
                BankAccountDetails = new() { BankAccountNumber = "40702810000000000001", BankDetails = new() { BankName = "ПАО Банк", BankId = "044525225", CorrespondentAccount = "30101810400000000225" } },
                Contact = new() { PhoneList = ["+7 495 000-00-00", "", "+7 495 000-00-01"], EmailList = ["a@b.ru"], OtherInfo = "иное" }
            };
            var buyer = new VS.Organization
            {
                OrganizationIdentificationInfo = new() { IndividualEntrepreneur = new() { Surname = "Иванов", FirstName = "Иван", Patronymic = "Иванович", Inn = "500100732259", IndividualEntrepreneurRegistrationCertificate = "Св-во 1", Ogrnip = "304500116000157", OgrnipDate = new DateTime(2004, 1, 2), OtherInfo = "иное ИП" } },
                Address = new()
                {
                    AddressCode = new()
                    {
                        UniqueCode = Guid.Parse("11111111-2222-3333-4444-555555555555"), ZipCode = "140000", Region = "50", RegionName = "Московская область",
                        MunicipalDistrict = new() { TypeCode = "2", Name = "Люберцы" }, UrbanRuralSettlement = new() { TypeCode = "1", Name = "Поселение" },
                        Settlement = new() { Type = "г.", Name = "Люберцы" }, PlanningStructureElement = new() { Type = "мкр.", Name = "Центр" },
                        StreetRoadNetworkElement = new() { Type = "ул.", Name = "Мира" }, LandPlotNumber = "12",
                        BuildingList = [new() { Type = "д.", Number = "5" }, new() { Type = "стр.", Number = "1" }],
                        BuildingRoom = new() { Type = "пом.", Number = "7" }, ApartmentRoom = new() { Type = "ком.", Number = "2" }
                    }
                }
            };
            var consignee = new VS.Organization
            {
                OrganizationIdentificationInfo = new() { ForeignPerson = new() { Status = "ИО", CountryCode = "112", CountryName = "Беларусь", Name = "ООО Минск", Identifier = "BY123", OtherInfo = "иное" } },
                Address = new() { AddressInformation = new() { CountryCode = "112", CountryName = "Беларусь", Address = "Минск, пр. Независимости, 1" } }
            };
            var mark = new VS.InvoiceItem
            {
                ProductName = "Кроссовки", UnitCode = "796", UnitName = "шт", Quantity = 2.5M, Price = 1000.456M, SumWithoutVat = 2501.14M, TaxRate = TaxRate.TwentyTwoPercent,
                Sum = 3051.39M, Excise = 10.5M, Vat = 550.25M,
                CustomsDeclarationList = [new() { CountryCode = "156", DeclarationNumber = "10702070/010426/0000001" }],
                AdditionalInfo = new()
                {
                    Type = VSR.InvoiceItemType.Property, AdditionalTypeInfo = "доп", ItemToRelease = 3.1234567M, Characteristic = "Хар", Kind = "1", Series = "S",
                    Article = "ART-1", Code = "K-1", GlobalTradeItemNumber = "04600000000001", CatalogCode = "КК", FeaccCode = "6404", ProductTypeCode = "630",
                    Okdp2ProductCode = "15.20", AdditionalOperationInfo = "ДИ", CountryNames = ["КИТАЙ"],
                    SupportingDocumentList = [Doc("Сертификат", "С-1") with { FileId = "F1", DocumentId = "D1", StorageSystemId = "ГИС", SystemUrl = "https://x.ru", OtherInfo = "доп", Creators = [new() { InnLegalEntity = "7777777777" }, new() { InnIndividual = "500100732259" }, new() { ForeignEntity = new() { Status = "ИГ", CountryCode = "112", CountryName = "Беларусь", Name = "Петров" } }, new() { AuthorityShortName = "ФНС" }] }],
                    TaxAccountingAmortization = new() { AmortizationGroup = "3", OkofCode = "330.28", EstablishedUsefulLife = "60", ActualUsefulLifeMonths = "12" },
                    RecoveredTaxAmount = new() { Amount = 5M, WithoutVat = false },
                    ItemIdentificationNumberList = [new() { PackageId = "SSCC1", MarkedProductQuantity = "2", ProductionBatchCode = "П1", MarkItems = ["010460000000000121abc", "010460000000000121def"] }, new() { SecondaryPackageItems = ["(00)046000000000000001"] }],
                    GovernmentSystemAdditionalInfoList = [new() { InformationSystemName = "ГИС МТ", AccountingUnit = "шт", OtherInfo = "иное", AccountingUnitIds = ["ID1", "ID2"] }]
                },
                OtherEconomicInfoItemList = [new() { Id = "Ключ", Value = "Значение" }]
            };
            var withoutVat = new VS.InvoiceItem { ProductName = "Услуга", TaxRate = TaxRate.WithoutVat, Sum = 100M, SumWithoutVat = 100M };

            var document = new VS.SellerUniversalTransferDocument
            {
                FileId = "ON_NSCHFDOPPR_2LT-1_2LT-2_20260402_00000000-0000-0000-0000-000000000001_0_1_0_0_0_00",
                SenderEdmParticipant = new EdmParticipant("2LT", "2"),
                RecipientEdmParticipant = new EdmParticipant("2LT", "1"),
                Function = UniversalTransferDocumentFunction.СЧФДОП,
                DocumentNumber = "15", DocumentDate = new DateTime(2026, 4, 1), DocumentUid = "0000000000-7777777777-000001",
                ApprovedStructureAdditionalInfoFields = "0000.0000.0000",
                DocumentCreator = seller, DocumentCreatorBase = Doc("Доверенность", "Д-1"),
                CurrencyCode = "643", CurrencyName = "Российский рубль", CurrencyRate = 1.5M,
                Sellers = [seller], Buyers = [buyer], Shippers = [seller], Consignees = [consignee],
                PaymentDocumentInfoList = [new() { Number = "1", Date = new DateTime(2026, 3, 30), Total = 100.5M }, new() { Number = "2" }],
                DocumentShipmentList = [Doc("УПД", "15")],
                AdditionalTransactionParticipantInfo = new()
                {
                    GovernmentContractInfo = "ГК-1", InvoiceFormationType = VSR.InvoiceFormationType.ItemsRealization, UPDInvoiceFormationType = "1", UPDFormationType = "2",
                    ObligationInfoList = [new() { Code = "01", Name = "Обязательство" }],
                    SellerInfoCircumPublicProc = new() { DateStateContract = new DateTime(2026, 1, 1), NumberStateContract = "ГК-1", PersonalAccountSeller = "ЛС", SellerBudgetClassCode = "БК", SellerTargetCode = "ЦК", SellerTreasuryCode = "КК", SellerTreasuryName = "УФК" },
                    FactorInfo = Legal("ООО Фактор", "7700000001", "770001001"),
                    MainAssignMonetaryClaim = Doc("Договор цессии", "Ц-1"),
                    SupportingDocumentList = [Doc("Спецификация", "1")]
                },
                OtherEconomicInfo = new VS.OtherEconomicInfo1 { InfoFileId = "11111111-0000-0000-0000-000000000000", Items = [new() { Id = "МЧД", Value = "111f11d1" }] },
                Items = [mark, withoutVat],
                TransferInfo = new()
                {
                    TransferDetails = new()
                    {
                        OperationName = "Товары переданы", OperationType = "Продажа", Date = new DateTime(2026, 4, 2), StartDate = new DateTime(2026, 4, 1), EndDate = new DateTime(2026, 4, 3),
                        TransferDocuments = [Doc("Договор", "Д-15")],
                        SenderPerson = new() { Employee = new() { Surname = "Петров", FirstName = "Петр", Patronymic = "Петрович", JobTitle = "Кладовщик", OtherInfo = "иное" } },
                        Transportation = new() { TransferTextInfo = "Авто", Incoterms = "EXW", IncotermsVersion = "2020" },
                        CreatedThingInfo = new() { Date = new DateTime(2026, 4, 2), Information = "передано", Document = Doc("Акт", "А-1") }
                    },
                    OtherEconomicInfo = new VS.OtherEconomicInfo3 { Items = [new() { Id = "3", Value = "три" }] }
                },
                Signers =
                [
                    new() { JobTitle = "Директор", SignatureType = VR.SignatureType.QualifiedElectronicSignature, SigningDate = new DateTime(2026, 4, 2), AuthorityConfirmationType = VR.AuthorityConfirmationType.SystemMachineReadablePoAWithDocInfo, AdditionalInfo = "доп", Person = new() { Surname = "Сидоров", FirstName = "Сидор", Patronymic = "Сидорович" },
                        ElectronicPoAInfo = new() { RegistrationNumber = Guid.Parse("111f11d1-1111-111a-1ff1-fa11e11afc11"), IssueDate = new DateTime(2026, 1, 1), InternalNumber = "1", InternalRegistrationDate = new DateTime(2026, 1, 2), SystemIdentification = "https://m4d.nalog.gov.ru/", SystemURL = "https://m4d.nalog.gov.ru/" } }
                ]
            };

            return document;
        }

        public static VS.SellerUniversalTransferDocument Others()
        {
            var physical = new VS.Organization { OrganizationIdentificationInfo = new() { PhysicalPerson = new() { Surname = "Кузнецов", FirstName = "Кузьма", Patronymic = "Кузьмич", Inn = "500100732260", Status = "1", OtherInfo = "паспорт" } }, Address = Moscow() };
            var ie = new VS.Organization { OrganizationIdentificationInfo = new() { IndividualEntrepreneur = new() { Surname = "Иванов", FirstName = "Иван", Patronymic = "Иванович", Inn = "500100732259" } }, Address = Moscow() };

            return new VS.SellerUniversalTransferDocument
            {
                FileId = "ON_NSCHFDOPPR_2LT-1_2LT-2_20260402_00000000-0000-0000-0000-000000000002_0_0_0_0_0_00",
                SenderEdmParticipant = new EdmParticipant("2LT", "2"),
                RecipientEdmParticipant = new EdmParticipant("2LT", "1"),
                Function = UniversalTransferDocumentFunction.СЧФ,
                DocumentNumber = "16", DocumentDate = new DateTime(2026, 4, 1),
                RevisionNumber = "1", RevisionDate = new DateTime(2026, 4, 5),
                CorrectedSellerFileName = "ON_NSCHFDOPPR_old", CorrectedBuyerFileName = "ON_NSCHFDOPPOK_old",
                DocumentCreator = ie,
                CurrencyCode = "643", CurrencyName = "Российский рубль",
                Sellers = [ie], Buyers = [physical],
                AdditionalTransactionParticipantInfo = new() { InvoiceFormationType = VSR.InvoiceFormationType.PaymentsRecieve, UPDInvoiceFormationType = "1", UPDFormationType = "2" },
                Items = [new() { ProductName = "Товар", TaxRate = TaxRate.TwentyPercent, Sum = 120M, Vat = 20M, Quantity = 1M, Price = 100M, SumWithoutVat = 100M }],
                TransferInfo = new()
                {
                    TransferDetails = new()
                    {
                        SenderPerson = new()
                        {
                            OtherIssuer = new()
                            {
                                OrganizationPerson = new() { Surname = "Орлов", FirstName = "Олег", JobTitle = "Водитель", OtherInfo = "иное", OrganizationName = "ООО Перевозчик", OrganizationInn = "7700000002", OrganizationBase = Doc("Договор перевозки", "П-1"), EmployeeBase = Doc("Доверенность", "Д-2") }
                            }
                        }
                    }
                },
                Signers =
                [
                    new() { AuthorityConfirmationType = VR.AuthorityConfirmationType.PaperBasedDocument, Person = new() { Surname = "Иванов", FirstName = "Иван", Patronymic = "Иванович" },
                        PaperPoAInfo = new() { IssueDate = new DateTime(2026, 2, 1), InternalNumber = "б/н", TrusteeInfo = "ООО", Signer = new() { Surname = "Петров", FirstName = "Петр", Patronymic = "Петрович" } } },
                    new() { AuthorityConfirmationType = VR.AuthorityConfirmationType.ElectronicSignatureData, Person = new() { Surname = "Иванов", FirstName = "Иван", Patronymic = "Иванович" } }
                ]
            };
        }

        /// <summary>
        /// УПДи с функцией СЧФДОП: к счету-фактуре (СЧФ) информации покупателя нет.
        /// </summary>
        public static VS.SellerUniversalTransferDocument OthersWithBuyerTitle()
        {
            var document = Others();
            document.Function = UniversalTransferDocumentFunction.СЧФДОП;

            return document;
        }

        public static VS.SellerUniversalTransferDocument Dop()
        {
            var seller = Legal("ООО «Продавец»", "7777777777", null);
            return new VS.SellerUniversalTransferDocument
            {
                FileId = "ON_NSCHFDOPPR_2LT-1_2LT-2_20260402_00000000-0000-0000-0000-000000000003_0_0_0_0_0_00",
                SenderEdmParticipant = new EdmParticipant("2LT", "2"),
                RecipientEdmParticipant = new EdmParticipant("2LT", "1"),
                Function = UniversalTransferDocumentFunction.ДОП,
                DocumentNumber = "17", DocumentDate = new DateTime(2026, 4, 1), DocumentName = "Накладная",
                DocumentCreator = seller,
                CurrencyCode = "643", CurrencyName = "Российский рубль",
                Sellers = [seller], Buyers = [Legal("ООО «Покупатель»", "7700000003", "770001003")],
                AdditionalTransactionParticipantInfo = new() { UPDFormationType = "2" },
                Items = [new() { ProductName = "Товар", TaxRate = TaxRate.TenFraction, Sum = 110M, Vat = 10M }],
                TransferInfo = new()
                {
                    TransferDetails = new()
                    {
                        SenderPerson = new() { OtherIssuer = new() { PhysicalPerson = new() { Surname = "Смирнов", FirstName = "Семен", PersonInn = "500100732261", Othernfo = "паспорт", PersonBase = Doc("Доверенность", "Д-3") } } }
                    }
                },
                Signers = [new() { JobTitle = "Директор", AuthorityConfirmationType = VR.AuthorityConfirmationType.ElectronicSignatureData, Person = new() { Surname = "Иванов", FirstName = "Иван", Patronymic = "Иванович" } }]
            };
        }
    }
}
