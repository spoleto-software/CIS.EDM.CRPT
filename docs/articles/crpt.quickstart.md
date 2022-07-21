# Быстрый старт

Клиент написан на C#, .NET Core 3.1 с использованием Dependency Injection от [Microsoft](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-usage).
# Определение зависимостей

```C#
services.AddHttpClient();
services.AddTransient<ICRPTTokenProvider, CRPTTokenProvider>();
services.AddTransient<IEdmProvider, CRPTProvider>();
```

# Инициализация сертификата и адреса сервиса ЦРПТ

```C#
var certificateThumbprint = "12345";

var option = new CRPTOption
{
	ServiceUrl = "https://markirovka.sandbox.crptech.ru/",
	CertificateThumbprint = certificateThumbprint
};
option.CertificateThumbprint = certificateThumbprint;
```

# Создание документа с информацией продавца
В данном примере максимально заполнены все свойства документа. В реальной ситуации необходимо указывать только нужные свойства.

```C#
private static SellerUniversalTransferDocument CreateSellerDataContract()
{
    var dataContract = new SellerUniversalTransferDocument
    {
        Function = UniversalTransferDocumentFunction.СЧФДОП,
        DocumentDate = DateTime.Parse("2022-08-01"),
        DocumentNumber = "53 (by API)",
        CurrencyCode = "643",
		
        SenderEdmParticipant = new EdmParticipant("2LT", "111"), 
        RecipientEdmParticipant = new EdmParticipant("2LT", "222"),
		
        EdmOperator = new()
        {
            OperatorId = "2LT",
            Name = "ООО \"Оператор-ЦРПТ\"",
            Inn = "7731376812"
        },
		
        Sellers = new()
        {
            new ()
            {
                OrganizationIdentificationInfo = new()
                {
                    LegalPerson = new()
                    {
                        Name = "ООО \"Продавец\"",
                        Inn = "7776666666",
                        Kpp = "772222222",
                    }
                },
                Okpo = "52573834",
                Department = "Департамент по умолчанию",
                Address = new()
                {
                    RussianAddress = new()
                    {
                        ZipCode = "123007",
                        RegionCode = "77",
                        Street = "Тверская",
                        Building = "2",
                        Block = "2"
                    }
                },
                Contact = new()
                {
                    Email = "test@test.ru",
                    Phone = "7 495 123 1234",
                },
                BankAccountDetails = new()
                {
                    BankAccountNumber = "123456",
                    BankDetails = new()
                    {
                        BankName = "Test bank",
                        BankId = "123456789",
                        CorrespondentAccount = "123456"
                    }
                }
            }
        },
		
        Shippers = new()
        {
            new()
            {
                OrganizationIdentificationInfo = new()
                {
                    LegalPerson = new()
                    {
                        Name = "ООО \"Продавец\"",
                        Inn = "7776666666",
                        Kpp = "772222222",
                    }
                },
                Okpo = "52575757",
                Department = "Департамент по умолчанию",
                Address = new()
                {
                    RussianAddress = new()
                    {
                        ZipCode = "123007",
                        RegionCode = "77",
                        Street = "Тверская",
                        Building = "2",
                        Block = "2"
                    }
                },
                Contact = new()
                {
                    Email = "test@test.ru",
                    Phone = "7 495 123 1234",
                },
                BankAccountDetails = new()
                {
                    BankAccountNumber = "123456",
                    BankDetails = new()
                    {
                        BankName = "Test bank",
                        BankId = "123456789",
                        CorrespondentAccount = "123456"
                    }
                }
            }
        },
		
        Buyers = new()
        {
            new()
            {
                OrganizationIdentificationInfo = new()
                {
                    LegalPerson = new()
                    {
                        Name = "ООО \"Рога и Копыта\"",
                        Inn = "7777777777",
                        Kpp = "771111111",
                    }
                },
                Okpo = "25469637",
                Address = new()
                {
                    RussianAddress = new()
                    {
                        ZipCode = "119270",
                        RegionCode = "77",
                        Street = "Тверская",
                        Building = "1",
                        Block = "1"
                    }
                },
                BankAccountDetails = new()
                {
                    BankAccountNumber = "40702810820000000147",
                    BankDetails = new()
                    {
                        BankName = "ФИЛИАЛ БАНКА",
                        BankId = "044111111",
                        CorrespondentAccount = "30111111111111111111"
                    }
                }
            }
        },
		
        Consignees = new()
        {
            new()
            {
                OrganizationIdentificationInfo = new()
                {
                    LegalPerson = new()
                    {
                        Name = "ООО \"Рога и Копыта\"",
                        Inn = "7777777777",
                        Kpp = "771111111",
                    }
                },
                Okpo = "25222222",
                Address = new()
                {
                    RussianAddress = new()
                    {
                        ZipCode = "119270",
                        RegionCode = "77",
                        Street = "Тверская",
                        Building = "1",
                        Block = "1"
                    }
                },
                BankAccountDetails = new()
                {
                    BankAccountNumber = "407111111111111111",
                    BankDetails = new()
                    {
                        BankName = "ФИЛИАЛ БАНКА",
                        BankId = "044111111",
                        CorrespondentAccount = "30111111111111111111"
                    }
                }
            }
        },
		
        Signers = new List<SellerSigner>
        {
            new ()
            {
                LegalPersonRepresentative =  new ()
                {
                    Surname = "Петров",
                    FirstName = "Пётр",
                    Patronymic = "Петрович",
                    JobTitle = "Генеральный директор",
                    Inn = "7777777777"
                }
            }
        },

        TransferInfo = new TransferInfo
        {
            Date = DateTime.Parse("2022-01-12"),
            SenderPerson = new()
            {
                Employee = new()
                {
                    JobTitle = "Кладовщик",
                    Surname = "Иванов",
                    FirstName = "Ваня",
                    Patronymic = "Иванович",
                }
            },
            Transportation = new()
            {
                TransferTextInfo = "Информация о транспортировке тут",
                Waybills = new()
                {
                    new Waybill { TransferDocumentDate = DateTime.Parse("2022-01-03"), TransferDocumentNumber = "тр.н. №1" },
                    new Waybill { TransferDocumentDate = DateTime.Parse("2022-01-05"), TransferDocumentNumber = "тр.н. №2" },
                },
            },
            TransferDocuments = new List<TransferDocument>()
            {
                new ()
                {
                    DocumentDate = DateTime.Parse("2022-01-02"),
                    DocumentName = "Документ об отгрузке",
                    DocumentNumber = "Док №111"
                }
            },
            CreatedThingInfo = new()
            {
                Information = "Инфа о созданной вещи",
                Date = DateTime.Parse("2022-01-01")
            },
            OperationType = "Операция по отгрузке наша",
            StartDate = DateTime.Parse("2022-01-01"),
            EndDate = DateTime.Parse("2022-03-02"),
            OtherEconomicInfo = new()
            {
                InfoFileId = Guid.NewGuid().ToString(),
                Items = new()
                {
                    new OtherEconomicInfoItem { Id = "АйдиДляОтгрузки", Value = "И тут Айди" }
                }
            }
        },
		
        DocumentShipmentList = new()
        {
            new TransferDocumentShipmentInfo
            {
                Date = DateTime.Parse("2021-01-12"),
                Name = "Документ об отгрузке 1",
                Number = "123459"
            },
            new TransferDocumentShipmentInfo
            {
                Date = DateTime.Parse("2021-01-13"),
                Name = "Документ об отгрузке 2",
                Number = "1234567"
            }
        },
		
        OtherEconomicInfo = new()
        {
            InfoFileId = Guid.NewGuid().ToString(),
            Items = new()
            {
                new OtherEconomicInfoItem { Id = "Айди", Value = "123" },
                new OtherEconomicInfoItem { Id = "Тест", Value = "Тест!" }
            }
        },
		
        PaymentDocumentInfoList = new()
        {
            new PaymentDocumentInfo
            {
                Date = DateTime.Parse("2021-12-15"),
                Number = "1",
                Total = 951000M
            },
            new PaymentDocumentInfo
            {
                Date = DateTime.Parse("2021-12-20"),
                Number = "2",
                Total = 356000M
            },
            new PaymentDocumentInfo
            {
                Date = DateTime.Parse("2021-12-23"),
                Number = "3"
            }
        },
		
        GovernmentContractInfo = "12345678901234567890",
        CurrencyRate = 1M,
        CurrencyName = "RUB",
        InvoiceFormationType = InvoiceFormationType.ItemsRealization,
        SellerInfoCircumPublicProc = new()
        {
            DateStateContract = DateTime.Parse("2021-12-01"),
            NumberStateContract = "111",
            PersonalAccountSeller = "12345678901",
            SellerBudgetClassCode = "44332211334455667788",
            SellerTargetCode = "54321234567890123456",
            SellerTreasuryCode = "999",
            SellerTreasuryName = "деревня Пестово"
        },
		
        MainAssignMonetaryClaim = new()
        {
            DocumentDate = DateTime.Parse("2021-11-02"),
            DocumentId = "55553",
            DocumentInfo = "Доп инфо тут",
            DocumentName = "Кастомный документ",
            DocumentNumber = "Уступка 1"
        },
        ApprovedStructureAdditionalInfoFields = "1234.5678.9872",
        DocumentCreatorBase = "Основания доверия",
        //RevisionNumber = "1",
        //RevisionDate = DateTime.Parse("2022-02-04")
    };

    dataContract.TransferInfo.Transportation.Carrier = dataContract.Sellers[0];
    dataContract.FactorInfo = dataContract.Sellers[0];
    dataContract.DocumentCreator = dataContract.Sellers[0];
    dataContract.Items = new()
    {
        new InvoiceItem
        {
            ProductName = "Пальто замшевое",
            Quantity = 1,
            Price = 100000,
            SumWithoutVat = 100000,
            Sum = 120000,
            Vat = 20000,
            TaxRate = TaxRate.TwentyPercent,
            UnitCode = "796",
            CustomsDeclarationList = new()
            {
                new CustomsDeclaration
                {
                    CountryCode = "643",
                    DeclarationNumber = "12345678/123456/1234567"
                },
                new CustomsDeclaration
                {
                    CountryCode = "643",
                    DeclarationNumber = "12345678/123456/456789"
                }
            },
            AdditionalInfo = new()
            {
                Article = "12345678",
                Code = "1234",
                Characteristic = "Хороший товар",
                ItemTracingInfoList = new()
                {
                    new InvoiceItemTracingInfo
                    {
                        RegNumberUnit = "123",
                        Quantity = 1,
                        UnitCode = "796",
                        AdditionalInfo = "доп информация."
                    },
                    new InvoiceItemTracingInfo
                    {
                        RegNumberUnit = "456",
                        Quantity = 1,
                        UnitCode = "796",
                        AdditionalInfo = "доп информация. #2"
                    }
                },
                ItemIdentificationNumberList = new()
                {
                    new()
                    {
                        SecondaryPackageItems = new() { "RU-12345-ABC12345", "123456" },
                        PackageId = "1122667788",
                    },
                    new()
                    {
                        MarkItems = new() { "RU-1234567-ABC123547", "11112222" },
                    }
                },
                FeaccCode = "8528599009",
            },
            OtherEconomicInfoItemList = new()
            {
                new OtherEconomicInfoItem { Id = "АйдиТовара", Value = "ТутАйди" },
                new OtherEconomicInfoItem { Id = "Признак", Value = "ИЗначениеПризнака" }
            }
        },
		
        new InvoiceItem
        {
            ProductName = "Трико замшевое",
            Quantity = 1,
            Price = 100000,
            SumWithoutVat = 100000,
            Sum = 120000,
            Vat = 20000,
            TaxRate = TaxRate.TwentyPercent,
            UnitCode = "796",
            CustomsDeclarationList = new()
            {
                new CustomsDeclaration
                {
                    CountryCode = "643",
                    DeclarationNumber = "10702030/261219/0041146"
                },
                new CustomsDeclaration
                {
                    CountryCode = "643",
                    DeclarationNumber = "15324/123/987654"
                }
            },
            AdditionalInfo = new()
            {
                Article = "12012025",
                Code = "1935",
                Characteristic = "Ну очень хороший товар",
                UnitName = "штука",
                Kind = "СОРТ№",
                Type = InvoiceItemType.Property,
                CatalogCode = "Каталожный код жлофыво лврп",
                AdditionalTypeInfo = "ИНФА",
                CountryName = "Россия",
                ItemTracingInfoList = new()
                {
                    new InvoiceItemTracingInfo
                    {
                        RegNumberUnit = "123",
                        Quantity = 1,
                        UnitCode = "796",
                        UnitName = "шт.",
                        AdditionalInfo = "доп информация."
                    },
                    new InvoiceItemTracingInfo
                    {
                        RegNumberUnit = "456",
                        Quantity = 1,
                        UnitCode = "796",
                        UnitName = "шт.",
                        AdditionalInfo = "доп информация. #2"
                    }
                },
                FeaccCode = "8528599009"
            }
        }
    };

    return dataContract;
}
```

# Отправка документа

```C#
var provider = services.GetRequiredService<IEdmProvider>();
var documentId = provider.PostUniversalTransferDocument(option, sellerDataContract);
```

Результатом выполнения метода ``PostUniversalTransferDocument`` будет идентификатор отправленного сообщения (``documentId``).

Помимо асинхронной версии метода есть еще и обычная версия ``PostUniversalTransferDocument``.

# Создание документа с информацией покупателя

```C#
private static BuyerUniversalTransferDocument CreateBuyerDataContract()
{
    var buyerDataContract = new BuyerUniversalTransferDocument
    {
        SenderEdmParticipant = new EdmParticipant("2LT", "222"), 
        RecipientEdmParticipant = new EdmParticipant("2LT", "111"),
		
        DocumentCreator = new()
        {
            OrganizationIdentificationInfo = new()
            {
                LegalPerson = new()
                {
                    Name = "ООО \"Рога и Копыта\"",
                    Inn = "7777777777",
                    Kpp = "771111111"
                }
            }
        },
		
        EdmOperator = new()
        {
            OperatorId = "2LT",
            Name = "ООО \"Оператор-ЦРПТ\"",
            Inn = "7731376812"
        },
		
        AcceptanceInfo = new()
        {
            Date = DateTime.Parse("2022-01-21"),
            ReceiverPerson = new()
            {
                Employee = new()
                {
                    JobTitle = "Кладовщик",
                    Surname = "Сидоров",
                    FirstName = "Сидр",
                    Patronymic = "Сидорович",
                }
            },
        },
		
        OtherEconomicInfo = new()
        {
            InfoFileId = Guid.NewGuid().ToString(),
            Items = new()
            {
                new OtherEconomicInfoItem { Id = "АйдиПолучения", Value = "Уникальное Айди" },
                new OtherEconomicInfoItem { Id = "ИЕщеАйди", Value = "Другое уникальное Айди" }
            }
        },
		
		BuyerInfoCircumPublicProc = new()
		{
			BuyerBudgetObligationAccountNumber = "1234567890123456",
			BuyerBudgetRegisterNumber = "11111111",
			BuyerFinancialAuthorityName = "Автор центр",
			PersonalAccountBuyer = "12345678902",
			BuyerMunicipalCode = "12345678",
			FinancialObligationInfoList = new List<FinancialObligationInfo>
			{
				new()
				{
					RowNumber=1,
					FundType= Models.Buyer.Reference.FundType.AdditionalBudget,
					BuyerBudgetClassCode = "12345678901234567890",
					AmountAdvance=999.9M
				}
			},
			BuyerTreasuryName = "деревня Калмыково",
			BuyerTreasuryCode = "6666",
		},

		Signers = new List<BuyerSigner>
        {
            new ()
            {
                LegalPersonRepresentative = new ()
                {
                    JobTitle = "Генеральный директор",
                    FirstName = "Марина",
                    Surname = "Фёдорова",
                    Patronymic = "Николаевна",
                    Inn = "7777777777",
                    OrgName = "ООО \"Рога и Копыта\"",
                    OtherInfo = "Прочая инфа о подписанте покупателя"
                }
            }
        },
		
        EdmDocumentId = "74cf35bd-290b-4c4d-b7e3-88ae4b3e4111"
    };
    return buyerDataContract;
}
```

Свойство ``EdmDocumentId`` содержит идентификатор сообщения продавца. На основе этого идентификатора автоматически заполнится часть свойств в документе покупателя, которые относятся к информации продавца.

# Подписание входящего документа

```C#
var provider = services.GetRequiredService<IEdmProvider>();
var documentId = await provider.ReceiptUniversalTransferDocumentAsync(option, buyerDataContract);
```

Результатом выполнения метода ``ReceiptUniversalTransferDocumentAsync`` будет идентификатор уведомления о получении сообщения (``PatchDocumentWithReceiptAsync``).

Помимо асинхронной версии метода есть еще и обычная версия ``ReceiptUniversalTransferDocument``.
