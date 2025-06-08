# Быстрый старт

Клиент написан на C#, .NET Core 3.1 с использованием Dependency Injection от [Microsoft](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-usage).
# Определение зависимостей

```csharp
services.AddHttpClient();
services.AddTransient<ICRPTTokenProvider, CRPTTokenProvider>();
services.AddTransient<IEdmProvider, CRPTProvider>();
```

# Инициализация сертификата и адреса сервиса ЦРПТ

```csharp
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

```csharp
private static SellerUniversalTransferDocument CreateSellerDataContract()
{
	var function = UniversalTransferDocumentFunction.ДОП;
	var docDate = DateTime.Parse("2023-11-24");
	var docNumber = "111 v.5.03 (by API) " + DateTime.Now.ToString("yyyyMMdd_HHmmss");
	var dataContract = new SellerUniversalTransferDocument
	{
		DocumentUid = "1234567890",
		Function = function,
		SenderEdmParticipant = new EdmParticipant("2LT", "111"),
		RecipientEdmParticipant = new EdmParticipant("2LT", "222"),
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
					EmailList = ["test@test.ru"],
					PhoneList = ["7 495 123 1234"],
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
					EmailList = ["test@test.ru"],
					PhoneList = ["7 495 123 1234"],
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
		Signers =
		[
			new ()
			{
                JobTitle = "Генеральный директор",
                AuthorityConfirmationType = Models.V5_03.Reference.AuthorityConfirmationType.ElectronicSignatureData,
                Person =  new ()
                {
                    Surname = "Петров",
                    FirstName = "Пётр",
                    Patronymic = "Петрович",
                },
			}
		],
		DocumentDate = docDate,
		DocumentNumber = docNumber,
		CurrencyCode = "643",
		AdditionalTransactionParticipantInfo = new()
		{
			GovernmentContractInfo = "12345678901234567890",
			InvoiceFormationType = InvoiceFormationType.СommissionAgentSales,
			UPDFormationType = "00006",
			MainAssignMonetaryClaim = new()
			{
				Date = DateTime.Parse("2021-11-02"),
				DocumentId = "55553",
				OtherInfo = "Доп инфо тут",
				Name = "Кастомный документ",
				Number = "Уступка 1"
			}
		},
		TransferInfo = new TransferInfo
		{
			TransferDetails = new()
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
				},
				TransferDocuments =
			[
				new ()
				{
					Date = DateTime.Parse("2022-01-02"),
					Name = "Документ об отгрузке",
					Number = "Док №111"
				}
			],
				CreatedThingInfo = new()
				{
					Information = "Инфа о созданной вещи",
					Date = DateTime.Parse("2022-01-01")
				},
				OperationType = "Операция по отгрузке наша",
				StartDate = DateTime.Parse("2022-01-01"),
				EndDate = DateTime.Parse("2022-03-02")
			},
			OtherEconomicInfo = new()
			{
				InfoFileId = Guid.NewGuid().ToString(),
				Items = new()
				{
					new OtherEconomicInfoItem { Id = "АйдиДляОтгрузки", Value = "И тут Айди" }
				}
			}
		},
		DocumentShipmentList =
		[
			new ()
			{
				Date = docDate,
				Name = function.GetDocumentName(),
				Number = docNumber
			},
			new ()
			{
				Date = docDate,
				Name = function.GetDocumentName(),
				Number = docNumber
			}
		],
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
		CurrencyRate = 1M,
		CurrencyName = "RUB",
		//SellerInfoCircumPublicProc = new()
		//{
		//    DateStateContract = DateTime.Parse("2021-12-01"),
		//    NumberStateContract = "111",
		//    PersonalAccountSeller = "12345678901",
		//    SellerBudgetClassCode = "44332211334455667788",
		//    SellerTargetCode = "54321234567890123456",
		//    SellerTreasuryCode = "999",
		//    SellerTreasuryName = "деревня Пестово"
		//},
		//ApprovedStructureAdditionalInfoFields = "1234.5678.9872",//todo:
		DocumentCreatorBase = new()
		{
			Name = "Основание доверия",
			Number = "123",
			Date = DateTime.Parse("2021-11-01")
		},
		//RevisionNumber = "1",
		//RevisionDate = DateTime.Parse("2022-02-04")
	};

	//dataContract.Shippers = dataContract.Sellers;
	//dataContract.Consignees = dataContract.Buyers;
	dataContract.AdditionalTransactionParticipantInfo.FactorInfo = dataContract.Sellers[0];
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
			UnitName = "штука",
			CustomsDeclarationList = new()
			{
				new CustomsDeclaration
				{
                    //CountryCode = "643",
                    DeclarationNumber = "12345678/123456/1234567"
				},
				new CustomsDeclaration
				{
                    //CountryCode = "643",
                    DeclarationNumber = "12345678/123456/456789"
				}
			},
			AdditionalInfo = new()
			{
					Article = "12345678",
					Code = "1234",
				Characteristic = "Хороший товар",
				CountryNames = [],
				//ItemTracingInfoList = new()
				//{
				//    new InvoiceItemTracingInfo
				//    {
				//        RegNumberUnit = "123",
				//        Quantity = 1,
				//        UnitCode = "796",
				//        AdditionalInfo = "доп информация."
				//    },
				//    new InvoiceItemTracingInfo
				//    {
				//        RegNumberUnit = "456",
				//        Quantity = 1,
				//        UnitCode = "796",
				//        AdditionalInfo = "доп информация. #2"
				//    }
				//},
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
			UnitName = "штука",
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
				Kind = "СОРТ№",
				Type = InvoiceItemType.Property,
				CatalogCode = "Каталожный код жлофыво лврп",
				AdditionalTypeInfo = "ИНФА",
				CountryNames = ["Россия", "Россия"],
				//ItemTracingInfoList = new()
				//{
				//    new InvoiceItemTracingInfo
				//    {
				//        RegNumberUnit = "123",
				//        Quantity = 1,
				//        UnitCode = "796",
				//        UnitName = "шт.",
				//        AdditionalInfo = "доп информация."
				//    },
				//    new InvoiceItemTracingInfo
				//    {
				//        RegNumberUnit = "456",
				//        Quantity = 1,
				//        UnitCode = "796",
				//        UnitName = "шт.",
				//        AdditionalInfo = "доп информация. #2"
				//    }
				//},
				//ItemIdentificationNumberList = new()
				//{
				//    new()
				//    {
				//        //ItemNumbers = new() { "RU-123-12345", "1234" },
				//        PackageId = "1122667788",
				//        SecondaryPackageIds = new() { "123456", "123" }
				//    },
				//    new() { ItemNumbers = new() { "RU-123-12345", "2222333" } }
				//},
				FeaccCode = "8528599009"
			}
		}
	};

    return dataContract;
}
```

# Отправка документа

```csharp
var provider = services.GetRequiredService<IEdmProvider>();
var documentId = await provider.PostUniversalTransferDocumentAsync(option, sellerDataContract);
```

Результатом выполнения метода ``PostUniversalTransferDocument`` будет идентификатор отправленного сообщения (``documentId``).

Помимо асинхронной версии метода есть еще и обычная версия ``PostUniversalTransferDocument``.

# Создание документа с информацией покупателя

```csharp
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

```csharp
var provider = services.GetRequiredService<IEdmProvider>();
var documentId = await provider.ReceiptUniversalTransferDocumentAsync(option, buyerDataContract);
```

Результатом выполнения метода ``ReceiptUniversalTransferDocumentAsync`` будет идентификатор уведомления о получении сообщения (``PatchDocumentWithReceiptAsync``).

Помимо асинхронной версии метода есть еще и обычная версия ``ReceiptUniversalTransferDocument``.
