# CIS.EDM.CRPT

[![](https://img.shields.io/github/license/spoleto-software/CIS.EDM.CRPT)](https://github.com/spoleto-software/CIS.EDM.CRPT/blob/main/LICENSE)
[![](https://img.shields.io/nuget/v/Spoleto.CIS.EDM.CRPT)](https://www.nuget.org/packages/Spoleto.CIS.EDM.CRPT/)
![Build](https://github.com/spoleto-software/CIS.EDM.CRPT/actions/workflows/publish-to-nuget.yml/badge.svg?branch=main)

The project for integration with CRPT EDM API (Electronic document management).

Интеграция с API ЭДО Lite от ЦРПТ.

## The provider settings
CRPTOption.ServiceUrl (string) - CRPT service address.

## TrueApiCRPTProvider

`TrueApiCRPTProvider` - вторая реализация `ICRPTProvider`: те же методы, но через методы «ЭДО Лайт» в True API (раздел 9), пакет Spoleto.TrueApi.Edo. Код `CRPT` теперь у него, а `CRPTProvider` устарел (`[Obsolete]`) и получил код `CRPT_Jwt`. Приложение, которое выбирает провайдера по `Code`, переходит на новый провайдер само, если тот зарегистрирован:

```csharp
services.AddHttpClient();
services.AddTransient<ICRPTProvider, TrueApiCRPTProvider>();
```

- Адрес True API - `CRPTOption.AuthUrl`: по нему и методы, и токен. `CRPTOption.ServiceUrl` - адрес прежнего API «ЭДО Лайт», этому провайдеру он не нужен.
- Токен - UUID, а у `CRPTProvider` - JWT, и провайдер токенов хранит один токен: не передавайте обоим провайдерам один экземпляр `ITrueApiTokenProvider`.
- УПД - только формат 970 (модели V5_03). Методы с моделями V5_01 (формат 820) бросают `NotSupportedException`.
- Списки документов: в True API статус и тип - одно значение, поэтому на каждое сочетание статусов и типов из `DocumentCollectionSearchModel` уходит свой запрос, а ответы объединяются и сортируются заново по времени создания. Каждый запрос получает свою страницу из `Limit` документов. Сортировка - только `created_at`, остальные поля бросают `NotSupportedException`.
- Даты - как у `CRPTProvider` с конвертерами CIS.EDM: даты документов в списках - местное время, а границы `CreatedFrom` и `CreatedTo` с `DateTimeKind.Unspecified` или `Local` считаются местным временем. Контрагент в `Document.Sender`: у входящих - отправитель, у исходящих - получатель.
- Отправка УПД и титул покупателя: ошибку, как и `CRPTProvider`, возвращают в `ResultInfo.Exception`, тогда `ResultInfo.Content` пустой. Титул покупателя берёт сведения о файле продавца из входящего документа `EdmDocumentId` и сам формирует имя своего файла, `SellerDocumentInfo` модели не заполняется.
- XML нового генератора совпадает со старым, кроме мест, где старый расходился с форматом 970 у информации покупателя: `ВидОпер` вместо `ВидОперации`, `РеквДокРасх` внутри `КодСодОпер`, без пустого `Должн` у подписанта, без пробела перед запятой в `НаимЭконСубСост` у ИП и физлица без отчества. Сверку со старым генератором делают тесты `CIS.EDM.CRPT.Tests`.
- Тесты `TrueApiCRPTProviderTests` работают без стенда. `TrueApiCRPTProviderStandTests` вызывают стенд True API: нужны КриптоПро, сертификат и раздел `TestOption` (`CertificateThumbprint`, `ServiceUrl` - адрес True API, `Inn`) в секретах пользователя. UserSecretsId у них тот же, что у тестов Spoleto.TrueApi, поэтому настроенный там стенд подходит и здесь. Отправка УПД, подписание черновика и титул покупателя меняют документы на стенде и запускаются только явно.

## Документация

Подробнее можно ознакомиться здесь:

https://spoleto-software.github.io/CIS.EDM.CRPT/