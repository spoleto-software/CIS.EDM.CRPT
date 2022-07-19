using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIS.EDM.CRPT.Models;
using CIS.EDM.CRPT.Providers;
using CIS.EDM.Diadoc.Models;
using CIS.EDM.Diadoc.Providers;
using CIS.EDM.Models;
using CIS.EDM.Models.Buyer;
using CIS.EDM.Models.Reference;
using CIS.EDM.Models.Seller;
using CIS.EDM.Models.Seller.Reference;
using CIS.EDM.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CIS.EDM.ConsoleApp
{
    internal class Program
    {
        const string sienaCertificateThumbprint = "4B9EB1BFFCF71B70AD77922D1E923D637585DAF6";
        const string valdoCertificateThumbprint = "8AD76C9C7178820CF56BAA9F57931CEBAFD3E9AC";

        static async Task Main(string[] args)
        {
            //await DiadocTest();
            await CRPTTest();

            Console.WriteLine("Hello World!");
        }

        private static async Task DiadocTest()
        {
            var builder = new HostBuilder()
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddHttpClient();
                   services.AddTransient<IEdmProvider, DiadocProvider>();
               }).UseConsoleLifetime();

            var host = builder.Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                try
                {


                    IEdmOption option = DiadocDemoOption;
                    option.CertificateThumbprint = sienaCertificateThumbprint;

                    var provider = services.GetRequiredService<IEdmProvider>();

                    //var criteria = new DocumentCollectionSearchModel
                    //{
                    //    Asc = true,
                    //    CreatedFrom = DateTime.Now,
                    //    PartnerInn = 123,
                    //    SortBy = SortField.type,
                    //    Limit = 20,
                    //    CreatedTo = DateTime.Now.AddDays(10),
                    //    Offset = 33,
                    //    Status = DocumentState.Sent,
                    //    Type = new System.Collections.Generic.List<DocumentType> { DocumentType.ClarificationNotice, DocumentType.ConfirmationOfReceiptDate }
                    //};
                    var sellerInn = "7714365994";
                    var buyerInn = "7704451997";
                    var date = DateTime.Parse("2022-01-21T14:36:45");
                    var dateFrom = date;//.Date;
                    var dateTo = date;//.AddSeconds(3);//.AddMinutes(1);//.Date.AddDays(1).AddSeconds(-1);

                    var searchModel = new DocumentCollectionSearchModel
                    {
                        PartnerInn = buyerInn,
                        CreatedFrom = dateFrom,
                        CreatedTo = dateTo,
                        Status = new() { DocumentState.DeliveredSignatureRequired, DocumentState.ReviewedSignatureRequired },
                    };
                    //var result0 = await provider.GetOutgoingDocumentListAsync(option, searchModel);
                    //var result1 = await provider.GetIncomingDocumentListAsync(option, searchModel);
                    //var result2 = await provider.GetOutgoingDocumentListAsync(option, criteria);

                    //var docId = "94ab67a8-6a73-4199-a811-f5c6f5a0e4e8";
                    //var xmlBody = await provider.GetOutgoingDocumentAsync(option, docId);
                    //await provider.SignOutgoingDocumentAsync(option, docId);

                    var sellerDataContract = CreateSellerDataContract();

                    var result = provider.PostUniversalTransferDocument(option, sellerDataContract);

                    var buyerDataContract = CreateBuyerDataContract();

                    option.CertificateThumbprint = valdoCertificateThumbprint;
                    var sign = await provider.ReceiptUniversalTransferDocumentAsync(option, buyerDataContract);

                    Console.WriteLine("Ok");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error Occured");
                }
            }
        }

        private static async Task CRPTTest()
        {
            var builder = new HostBuilder()
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddHttpClient();
                   services.AddTransient<ICRPTTokenProvider, CRPTTokenProvider>();
                   services.AddTransient<IEdmProvider, CRPTProvider>();
               }).UseConsoleLifetime();

            var host = builder.Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                try
                {
                    IEdmOption option = CRPTDemoOption;
                    option.CertificateThumbprint = sienaCertificateThumbprint;

                    var provider = services.GetRequiredService<IEdmProvider>();

                    //var criteria = new DocumentCollectionSearchModel
                    //{
                    //    Asc = true,
                    //    CreatedFrom = DateTime.Now,
                    //    PartnerInn = 123,
                    //    SortBy = SortField.type,
                    //    Limit = 20,
                    //    CreatedTo = DateTime.Now.AddDays(10),
                    //    Offset = 33,
                    //    Status = DocumentState.Sent,
                    //    Type = new System.Collections.Generic.List<DocumentType> { DocumentType.ClarificationNotice, DocumentType.ConfirmationOfReceiptDate }
                    //};
                    var sellerInn = "7714365994";
                    var buyerInn = "7704451997";
                    var date = DateTime.Parse("2022-01-21T14:36:45");
                    var dateFrom = date;//.Date;
                    var dateTo = date;//.AddSeconds(3);//.AddMinutes(1);//.Date.AddDays(1).AddSeconds(-1);

                    var searchModel = new DocumentCollectionSearchModel
                    {
                        PartnerInn = buyerInn,
                        CreatedFrom = dateFrom,
                        CreatedTo = dateTo,
                        Status = new() { DocumentState.DeliveredSignatureRequired, DocumentState.ReviewedSignatureRequired },
                    };
                    //var result0 = await provider.GetOutgoingDocumentListAsync(option, searchModel);
                    //var result1 = await provider.GetIncomingDocumentListAsync(option, searchModel);
                    //var result2 = await provider.GetOutgoingDocumentListAsync(option, criteria);

                    //var docId = "94ab67a8-6a73-4199-a811-f5c6f5a0e4e8";
                    //var xmlBody = await provider.GetOutgoingDocumentAsync(option, docId);
                    //await provider.SignOutgoingDocumentAsync(option, docId);

                    var sellerDataContract = CreateSellerDataContract();

                    //var result = provider.PostUniversalTransferDocument(option, sellerDataContract);

                    var buyerDataContract = CreateBuyerDataContract();

                    option.CertificateThumbprint = valdoCertificateThumbprint;
                    var sign = await provider.ReceiptUniversalTransferDocumentAsync(option, buyerDataContract);

                    Console.WriteLine("Ok");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error Occured");
                }
            }
        }

        private static BuyerUniversalTransferDocument CreateBuyerDataContract()
        {
            var buyerDataContract = new BuyerUniversalTransferDocument
            {
                SenderEdmParticipant = new EdmParticipant("2LT", "600000826"), //valdo
                RecipientEdmParticipant = new EdmParticipant("2LT", "106"), //siena
                DocumentCreator = new()
                {
                    OrganizationIdentificationInfo = new()
                    {
                        LegalPerson = new()
                        {
                            Name = "ООО \"ВАЛЬДО\"",
                            Inn = "7704451997",
                            Kpp = "770401001"
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
                            Surname = "Петров",
                            FirstName = "Петя",
                            Patronymic = "Петрович",
                        }
                    },
                },
                OtherEconomicInfo = new()
                {
                    InfoFileId = Guid.NewGuid().ToString(),
                    Items = new()
                    {
                        new OtherEconomicInfoItem { Id = "АйдиДляПолучения", Value = "И тут Тоже своё уникальное Айди" },
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
                            FirstName = "Лариса",
                            Surname = "Макаренко",
                            Patronymic = "Лариса",
                            Inn = "7704451997",
                            OrgName = "ООО \"ВАЛЬДО\"",
                            OtherInfo = "Прочая инфа о подписанте покупателя"
                        }
                    }
                },
                EdmDocumentId = "e10cbbc6-810a-4448-b35a-1bb19712271c"
            };
            return buyerDataContract;
        }

        private static SellerUniversalTransferDocument CreateSellerDataContract()
        {
            var dataContract = new SellerUniversalTransferDocument
            {
                EdmOperator = new()
                {
                    OperatorId = "2LT",
                    Name = "ООО \"Оператор-ЦРПТ\"",
                    Inn = "7731376812"
                },
                Function = UniversalTransferDocumentFunction.СЧФДОП,
                SenderEdmParticipant = new EdmParticipant("2LT", "106"), //siena
                RecipientEdmParticipant = new EdmParticipant("2LT", "600000826"), //valdo
                Sellers = new()
                {
                    new ()
                    {
                        OrganizationIdentificationInfo = new()
                        {
                            LegalPerson = new()
                            {
                                Name = "ООО \"СИЕНА\"",
                                Inn = "7714365994",
                                Kpp = "771401001",
                            }
                        },
                        Okpo = "52573834",
                        Department = "Департамент по умолчанию",
                        Address = new()
                        {
                            RussianAddress = new()
                            {
                                ZipCode = "123007",
                                Region = "77",
                                Street = "ул. Магистральная 3-я",
                                Building = "12",
                                Block = "1"
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
                                CorrespondentAccount = "987654"
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
                                Name = "ООО \"СИЕНА\"",
                                Inn = "7714365994",
                                Kpp = "771401001",
                            }
                        },
                        Okpo = "52573834",
                        Department = "Департамент по умолчанию",
                        Address = new()
                        {
                            RussianAddress = new()
                            {
                                ZipCode = "123007",
                                Region = "77",
                                Street = "ул. Магистральная 3-я",
                                Building = "12",
                                Block = "1"
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
                                CorrespondentAccount = "987654"
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
                                Name = "Общество с ограниченной ответственностью \"ВАЛЬДО\"",
                                Inn = "7704451997",
                                Kpp = "770401001",
                            }
                        },
                        Okpo = "25469637",
                        Address = new()
                        {
                            RussianAddress = new()
                            {
                                ZipCode = "119270",
                                Region = "77",
                                Street = "Лужнецкая наб",
                                Building = "2/4",
                                Block = "23Б, этаж/офис 2/214"
                            }
                        },
                        BankAccountDetails = new()
                        {
                            BankAccountNumber = "40702810820000000147",
                            BankDetails = new()
                            {
                                BankName = "ФИЛИАЛ \"ЦЕНТРАЛЬНЫЙ\" БАНКА ВТБ (ПАО) (Расчетный)",
                                BankId = "044525411",
                                CorrespondentAccount = "30101810145250000411"
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
                                Name = "Общество с ограниченной ответственностью \"ВАЛЬДО\"",
                                Inn = "7704451997",
                                Kpp = "770401001",
                            }
                        },
                        Okpo = "25469637",
                        Address = new()
                        {
                            RussianAddress = new()
                            {
                                ZipCode = "119270",
                                Region = "77",
                                Street = "Лужнецкая наб",
                                Building = "2/4",
                                Block = "23Б, этаж/офис 2/214"
                            }
                        },
                        BankAccountDetails = new()
                        {
                            BankAccountNumber = "40702810820000000147",
                            BankDetails = new()
                            {
                                BankName = "ФИЛИАЛ \"ЦЕНТРАЛЬНЫЙ\" БАНКА ВТБ (ПАО)",
                                BankId = "044525411",
                                CorrespondentAccount = "30101810145250000411"
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
                            Surname = "Колдоркин",
                            FirstName = "Александр",
                            Patronymic = "Викторович",
                            JobTitle = "Генеральный директор",
                            Inn = "7714365994"
                        }
                    }
                },
                DocumentDate = DateTime.Parse("2022-08-01"),
                DocumentNumber = "53 (by API)",
                CurrencyCode = "643",
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
                        Name = "Документик об отгрузке 1",
                        Number = "123459"
                    },
                    new TransferDocumentShipmentInfo
                    {
                        Date = DateTime.Parse("2021-01-13"),
                        Name = "Документик об отгрузке 2",
                        Number = "987651"
                    }
                },
                OtherEconomicInfo = new()
                {
                    InfoFileId = Guid.NewGuid().ToString(),
                    Items = new()
                    {
                        new OtherEconomicInfoItem { Id = "НашАйдишник", Value = "123" },
                        new OtherEconomicInfoItem { Id = "ТутПростоТест", Value = "Тест тут!" }
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
                DocumentCreatorBase = "Основания сильного доверия",
                //RevisionNumber = "1",
                //RevisionDate = DateTime.Parse("2022-02-04")
            };

            //dataContract.Shippers = dataContract.Sellers;
            //dataContract.Consignees = dataContract.Buyers;
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
                        Article = "12012022",
                        Code = "1932",
                        Characteristic = "Очень хороший товар",
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
                                SecondaryPackageItems = new() { "RU-430302-ABC12345", "951753" },
                                PackageId = "1122667788",
                            },
                            new()
                            {
                                MarkItems = new() { "RU-156123-ABC98765", "11112222" },
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
                        //ItemIdentificationNumberList = new()
                        //{
                        //    new()
                        //    {
                        //        //ItemNumbers = new() { "RU-430302-ABC12345", "951753" },
                        //        PackageId = "1122667788",
                        //        SecondaryPackageIds = new() { "123456", "988754" }
                        //    },
                        //    new() { ItemNumbers = new() { "RU-156123-ABC98765", "2222333" } }
                        //},
                        FeaccCode = "8528599009"
                    }
                }
            };

            return dataContract;
        }

        private static readonly CRPTOption CRPTDemoOption = new()
        {
            ServiceUrl = "https://edo.sandbox.crptech.ru/"// "https://edo.sandbox.crpt.tech/",
        };

        private static readonly DiadocOption DiadocDemoOption = new()
        {
            ApiClientId = "",
            ServiceUrl = "https://diadoc-api.kontur.ru/",
            Login = "ag@cashmere.ru",
            Password = "47127Hoigckbmb"
        };
    }
}
