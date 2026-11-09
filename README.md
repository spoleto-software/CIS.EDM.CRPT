# CIS.EDM.CRPT

[![](https://img.shields.io/github/license/spoleto-software/CIS.EDM.CRPT)](https://github.com/spoleto-software/CIS.EDM.CRPT/blob/main/LICENSE)
[![](https://img.shields.io/nuget/v/Spoleto.CIS.EDM.CRPT)](https://www.nuget.org/packages/Spoleto.CIS.EDM.CRPT/)
![Build](https://github.com/spoleto-software/CIS.EDM.CRPT/actions/workflows/publish-to-nuget.yml/badge.svg?branch=main)

The project for integration with CRPT EDM API (Electronic document management).

Интеграция с API ЭДО Lite от ЦРПТ.

## The provider settings
CRPTOption.ServiceUrl (string) - CRPT service address.

## TrueApiCRPTProvider

`TrueApiCRPTProvider` - вторая реализация `ICRPTProvider`: те же методы, но через методы «ЭДО Лайт» в True API (раздел 9), пакет [Spoleto.TrueApi.Edo](#spoletotrueapiedo) из этого же репозитория. Код `CRPT` теперь у него, а `CRPTProvider` устарел (`[Obsolete]`) и получил код `CRPT_Jwt`. Приложение, которое выбирает провайдера по `Code`, переходит на новый провайдер само, если тот зарегистрирован:

```csharp
services.AddHttpClient();
services.AddTransient<ICRPTProvider, TrueApiCRPTProvider>();
```

- Адрес True API - `CRPTOption.AuthUrl`: по нему и методы, и токен. `CRPTOption.ServiceUrl` - адрес прежнего API «ЭДО Лайт», этому провайдеру он не нужен.
- Токен - UUID, а у `CRPTProvider` - JWT, и провайдер токенов хранит один токен: не передавайте обоим провайдерам один экземпляр `ITrueApiTokenProvider`.
- УПД - только формат 970 (модели V5_03). Методы с моделями V5_01 (формат 820) бросают `NotSupportedException`.
- Списки документов: в True API статус и тип - одно значение, поэтому на каждое сочетание статусов и типов из `DocumentCollectionSearchModel` уходит свой запрос, а ответы объединяются и сортируются заново по времени создания. Каждый запрос получает свою страницу из `Limit` документов. Если в `Type` все типы, отбора по типу нет: уходит один запрос, а документы типов, которых нет в CIS.EDM (договоры с оператором ГИС МТ), отбрасываются. Сортировка - только `created_at`, остальные поля бросают `NotSupportedException`.
- Даты - как у `CRPTProvider` с конвертерами CIS.EDM: даты документов в списках - местное время, а границы `CreatedFrom` и `CreatedTo` с `DateTimeKind.Unspecified` или `Local` считаются местным временем. Контрагент в `Document.Sender`: у входящих - отправитель, у исходящих - получатель.
- Отправка УПД и титул покупателя: ошибку, как и `CRPTProvider`, возвращают в `ResultInfo.Exception`, а XML, который не принял сервис, - в `ResultInfo.Content`. Если ошибка случилась до формирования XML (например, не скачался файл продавца для титула), `ResultInfo.Content` - пустая строка. Титул покупателя берёт сведения о файле продавца из входящего документа `EdmDocumentId` и сам формирует имя своего файла, `SellerDocumentInfo` модели не заполняется.
- XML нового генератора совпадает со старым, кроме мест, где старый расходился с форматом 970 у информации покупателя: `ВидОпер` вместо `ВидОперации`, `РеквДокРасх` внутри `КодСодОпер`, без пустого `Должн` у подписанта, без пробела перед запятой в `НаимЭконСубСост` у ИП и физлица без отчества. Сверку со старым генератором делают тесты `CIS.EDM.CRPT.Tests`.
- Тесты `TrueApiCRPTProviderTests` и `TrueApiEdoProviderStubTests` работают без стенда. `TrueApiCRPTProviderStandTests` и `TrueApiEdoProviderTests` вызывают стенд True API: нужны КриптоПро, сертификат и раздел `TestOption` (`CertificateThumbprint`, `ServiceUrl` - адрес True API, `Inn`) в секретах пользователя. UserSecretsId у них тот же, что у тестов Spoleto.TrueApi, поэтому настроенный там стенд подходит и здесь. Отправка УПД, подписание черновика и титул покупателя меняют документы на стенде и запускаются только явно.

## Spoleto.TrueApi.Edo

**Spoleto.TrueApi.Edo** is an unofficial library to work with «ЭДО Лайт» methods of [True API](https://docs.crpt.ru/gismt/True_API/).

Пакет перенесён сюда из [Spoleto.TrueApi](https://github.com/spoleto-software/Spoleto.TrueApi) и объявлен устаревшим (deprecated) вместе с CIS.EDM.CRPT: `TrueApiEdoProvider` помечен `[Obsolete]`. Замена - `TrueApiEdmProvider` из пакета Spoleto.Edm.TrueApi ([Spoleto.Edm](https://github.com/spoleto-software/Spoleto.Edm)). Идентификатор пакета и пространства имён `Spoleto.TrueApi.Edo` прежние.

```bash
Install-Package Spoleto.TrueApi.Edo
```

Пакет Spoleto.CIS.EDM.CRPT зависит от Spoleto.TrueApi.Edo той же сборки, поэтому оба пакета публикуются вместе.

Запросы идут через [Spoleto.RestClient](https://github.com/spoleto-software/Spoleto.RestClient), ошибки - как у провайдеров Spoleto.TrueApi: `HttpRequestException` с кодом ответа в `StatusCode` и текстом ошибки сервиса, на 401 провайдер один раз повторяет запрос с новым токеном. Отправка УПД и титула покупателя: если файл уже сформирован, а подпись или загрузка не удались, - `EdoPostDocumentException`. В нём файл (`FileId`, `Content`, `RawContent`), текст ошибки - из `InnerException`, там же исходное `HttpRequestException`. Ошибка до формирования файла - как есть.

```csharp
var settings = new TrueApiEdoProviderOption
{
    ServiceUrl = "https://markirovka.crpt.ru",
    CertificateThumbprint = "thumbprint",
    Inn = "7777777777"
};

using var edoProvider = new TrueApiEdoProvider();

// Список входящих документов:
var list = await edoProvider.GetIncomingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 10, Status = EdoDocumentStatus.DeliveredSignatureRequired });
// Даты, как в CIS.EDM: в ответе - местное время, а CreatedFrom и CreatedTo без DateTimeKind.Utc считаются местным временем.

// Файл входящего документа как есть: XML для УПД, УКД и квитанций (для УПД - windows-1251), PDF для договоров с оператором ГИС МТ:
var file = await edoProvider.GetIncomingDocumentAsync(settings, list.Items[0].Documents[0].Id);

// XML читается в кодировке из своего заголовка. Для windows-1251 в .NET один раз на старте нужно зарегистрировать кодовые страницы:
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var xml = XDocument.Load(new MemoryStream(file));

// Исходящие документы - те же методы с Outgoing:
var outgoingList = await edoProvider.GetOutgoingDocumentListAsync(settings, new EdoDocumentSearchModel { Limit = 10 });
var outgoingFile = await edoProvider.GetOutgoingDocumentAsync(settings, outgoingList.Items[0].Documents[0].Id);

// Файл документа с открепленной подписью из ZIP-архива документа (архив есть только у УПД, УПДи, УКД и УКДи):
var signed = await edoProvider.GetIncomingSignedDocumentInfoAsync(settings, list.Items[0].Documents[0].Id);
var xml = signed.Content; // XML строкой, кодировку из заголовка библиотека учла сама; FileName - ИдФайл
var signature = Convert.ToBase64String(signed.Signature); // RawContent и Signature - файлы .xml и .p7s как есть, подпись сделана по RawContent
var outgoingSigned = await edoProvider.GetOutgoingSignedDocumentInfoAsync(settings, outgoingList.Items[0].Documents[0].Id);

// Сведения из файла информации продавца УПД / УПДи (формат 970), которые повторяет информация покупателя:
var sellerInfo = await edoProvider.GetIncomingSellerDocumentInfoAsync(settings, list.Items[0].Documents[0].Id);
var sellerFileId = sellerInfo.FileId; // ИдФайл, например, для запроса квитанций результата обработки
var outgoingSellerInfo = await edoProvider.GetOutgoingSellerDocumentInfoAsync(settings, outgoingList.Items[0].Documents[0].Id);

// Подписание черновика исходящего УПД / УПДи / УКД / УКДи сертификатом CertificateThumbprint, после чего документ уходит получателю.
// Библиотека скачивает файл черновика и подписывает его байты как есть открепленной подписью:
var drafts = await edoProvider.GetOutgoingDocumentListAsync(settings, new EdoDocumentSearchModel { Status = EdoDocumentStatus.Draft });
await edoProvider.SignOutgoingDocumentAsync(settings, drafts.Items[0].Documents[0].Id);

// Отправка УПД формата 970: библиотека формирует XML (windows-1251), подписывает его и загружает.
// С RevisionNumber документ уходит как УПДи. С isDraft: true - черновик без подписи, его потом отправляет SignOutgoingDocumentAsync.
var seller = new EdoOrganization
{
    OrganizationIdentificationInfo = new() { LegalPerson = new() { Name = "ООО «Продавец»", Inn = "7777777777", Kpp = "777701001" } },
    Address = new() { RussianAddress = new() { RegionCode = "77", Region = "г. Москва" } }
};
var upd = new EdoSellerUniversalTransferDocument
{
    SenderEdoId = "2LT-0000000001", // идентификаторы участников ЭДО входят в имя файла, ИдФайл библиотека формирует сама
    RecipientEdoId = "2LT-0000000002",
    Function = EdoDocumentFunction.InvoiceAndPrimaryDocument,
    DocumentNumber = "15",
    DocumentDate = new DateTime(2026, 4, 1),
    DocumentCreator = seller,
    Sellers = [seller],
    Buyers = [new() { OrganizationIdentificationInfo = new() { LegalPerson = new() { Name = "ООО «Покупатель»", Inn = "0000000000", Kpp = "000001001" } } }],
    Items =
    [
        new()
        {
            ProductName = "Кроссовки", UnitCode = "796", UnitName = "шт", Quantity = 1, Price = 100, SumWithoutVat = 100,
            TaxRate = EdoTaxRate.TwentyTwoPercent, Vat = 22, Sum = 122,
            AdditionalInfo = new() { ItemIdentificationNumberList = [new() { MarkItems = ["010460000000000121abc"] }] }
        }
    ],
    TransferInfo = new() { TransferDetails = new() { Date = new DateTime(2026, 4, 2) } },
    Signers = [new() { JobTitle = "Директор", AuthorityConfirmationType = EdoAuthorityConfirmationType.ElectronicSignatureData, Person = new() { Surname = "Иванов", FirstName = "Иван" } }]
};
var posted = await edoProvider.PostUniversalTransferDocumentAsync(settings, upd);
var updId = posted.Id; // FileId - ИдФайл, Content и RawContent - отправленный XML

// Титул покупателя к входящему УПД / УПДи формата 970: библиотека скачивает файл продавца, берёт из него реквизиты, подпись,
// отправителя и получателя, формирует XML, подписывает и загружает. Метод для УПД или УПДи выбирается по файлу продавца.
var buyerTitle = new EdoBuyerUniversalTransferDocument
{
    DocumentId = list.Items[0].Documents[0].Id, // входящий УПД, который ждёт подписи
    DocumentCreator = new() { OrganizationIdentificationInfo = new() { LegalPerson = new() { Name = "ООО «Покупатель»", Inn = "0000000000", Kpp = "000001001" } } },
    AcceptanceInfo = new() { OperationName = "Товары приняты без претензий", Date = new DateTime(2026, 4, 3) }, // по умолчанию - приняты без расхождений
    Signers = [new() { JobTitle = "Директор", AuthorityConfirmationType = EdoAuthorityConfirmationType.ElectronicSignatureData, Person = new() { Surname = "Петров", FirstName = "Петр" } }]
};
var receipt = await edoProvider.ReceiptUniversalTransferDocumentAsync(settings, buyerTitle);
var titleId = receipt.Id; // идентификатор, который вернул сервис; сам документ остаётся под прежним идентификатором
// УПД с кодами маркировки после титула покупателя уходит в ГИС МТ: статус «Подписан: Отправлен в ГИС МТ». УПД без кодов остаётся «Подписан».
```

## Документация

Подробнее можно ознакомиться здесь:

https://spoleto-software.github.io/CIS.EDM.CRPT/