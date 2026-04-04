# YooMoney API для ProxyAccessHub

## 1. Источник сведений

Этот файл собран по официальной документации YooMoney и включает только те API и сценарии, которые реально нужны `ProxyAccessHub` для MVP продления подписки.

Основные официальные источники:

- API ЮMoney для сбора денег: https://yoomoney.ru/docs/payment-buttons
- Сценарий работы с нестандартной формой: https://yoomoney.ru/docs/payment-buttons/using-api/flow
- Форма для перевода: https://yoomoney.ru/docs/payment-buttons/using-api/forms
- HTTP-уведомления: https://yoomoney.ru/docs/payment-buttons/using-api/notifications
- Регистрация OAuth-приложения: https://yoomoney.ru/docs/wallet/using-api/authorization/register-client
- Получение OAuth-токена: https://yoomoney.ru/docs/wallet/using-api/authorization/obtain-access-token
- Права токена: https://yoomoney.ru/docs/wallet/using-api/authorization/protocol-rights
- Метод `account-info`: https://yoomoney.ru/docs/wallet/user-account/account-info
- Метод `operation-history`: https://yoomoney.ru/docs/wallet/user-account/operation-history

Ниже описан только тот набор, который нужен для `ProxyAccessHub`. Все подряд API YooMoney сюда сознательно не включены.

## 2. Что реально нужно ProxyAccessHub

Для нашего проекта важны только два обязательных блока и один дополнительный:

### 2.1. Обязательный блок для MVP

- собственная HTML-форма оплаты;
- HTTP-уведомления YooMoney;
- привязка платежа к локальной заявке через `label`.

### 2.2. Дополнительный блок для надёжности и ручной сверки

- OAuth-приложение кошелька YooMoney;
- метод `account-info`;
- метод `operation-history`.

### 2.3. Что пока не нужно

Для текущего сценария `ProxyAccessHub` не нужны:

- конструктор кнопок и виджетов;
- `request-payment`;
- `process-payment`;
- формы оплаты товаров и услуг;
- магазинные паттерны и merchant-сценарии.

Причина простая: в MVP мы не проводим платёж через серверный wallet API от имени пользователя, а создаём свою форму перевода в кошелёк YooMoney и принимаем HTTP-уведомление о факте поступления денег.

## 3. Базовый сценарий для ProxyAccessHub

Официальный сценарий YooMoney для нестандартной формы выглядит так:

1. Пользователь выбирает способ оплаты.
2. Сайт отправляет параметры формы методом `POST` на `https://yoomoney.ru/quickpay/confirm`.
3. Пользователь подтверждает перевод на стороне YooMoney.
4. Деньги поступают в кошелёк получателя.
5. Сайт получает HTTP-уведомление.
6. Сайт анализирует сумму и `label` и решает, что делать дальше.

Для `ProxyAccessHub` это означает:

1. Мы создаём локальную заявку на оплату.
2. В `label` кладём наш внутренний `requestId`.
3. Рендерим свою HTML-форму.
4. После уведомления ищем заявку по `label`.
5. Проверяем подпись уведомления.
6. Пополняем баланс пользователя и запускаем расчёт продления.

Источник:

- https://yoomoney.ru/docs/payment-buttons/using-api/flow

## 4. Нестандартная HTML-форма оплаты

### 4.1. Адрес и метод

- метод: `POST`
- адрес: `https://yoomoney.ru/quickpay/confirm`

Источник:

- https://yoomoney.ru/docs/payment-buttons/using-api/forms

### 4.2. Обязательные поля формы

- `receiver` — номер кошелька YooMoney, куда должны прийти деньги;
- `quickpay-form` — фиксированное значение `button`;
- `paymentType` — способ оплаты:
  - `PC` — из кошелька YooMoney;
  - `AC` — с банковской карты;
- `sum` — сумма перевода.

### 4.3. Необязательные, но важные для нас поля

- `label` — метка платежа до 64 символов;
- `successURL` — URL для возврата пользователя после оплаты.

### 4.4. Что важно для ProxyAccessHub

- `label` — ключевое поле для связки внешнего перевода с нашей локальной заявкой;
- сумму нельзя брать из клиента как доверенную бизнес-истину после уведомления, её нужно сверять с ожидаемой суммой заявки;
- пользователь может оплатить из кошелька или с карты, поэтому в серверной логике нельзя завязываться только на один тип источника;
- `successURL` полезен только для UX, но не подтверждает оплату сам по себе;
- факт оплаты должен подтверждаться только HTTP-уведомлением и серверной обработкой.

### 4.5. Минимальный пример формы для ProxyAccessHub

```html
<form method="POST" action="https://yoomoney.ru/quickpay/confirm">
    <input type="hidden" name="receiver" value="41001xxxxxxxxxxxx" />
    <input type="hidden" name="quickpay-form" value="button" />
    <input type="hidden" name="label" value="renew-request-123" />
    <input type="hidden" name="sum" value="100.00" />
    <input type="hidden" name="successURL" value="https://example.com/user/payment/success" />

    <label><input type="radio" name="paymentType" value="PC" /> ЮMoney</label>
    <label><input type="radio" name="paymentType" value="AC" /> Банковская карта</label>
    <button type="submit">Оплатить</button>
</form>
```

Источник:

- https://yoomoney.ru/docs/payment-buttons/using-api/forms

## 5. Комиссия и сумма

По официальной документации при работе через форму есть разные коэффициенты комиссии:

- для `PC` комиссия считается по коэффициенту `0.01`;
- для `AC` комиссия считается по коэффициенту `0.03`.

Практический вывод для `ProxyAccessHub`:

- если мы хотим получить фиксированную сумму на кошелёк, нужно отдельно учитывать комиссию YooMoney;
- если логика проекта строится на сумме фактического зачисления, тогда в обработке уведомления нужно опираться на `amount`, а не на желаемую сумму формы;
- для MVP проще и безопаснее считать, что мы ожидаем конкретную сумму поступления в кошелёк и сравниваем именно её.

Источник:

- https://yoomoney.ru/docs/payment-buttons/using-api/forms

## 6. HTTP-уведомления о переводах

### 6.1. Формат

Уведомление отправляется:

- методом `POST`;
- в формате `application/x-www-form-urlencoded`;
- в кодировке `UTF-8`.

YooMoney делают до трёх попыток доставки:

- сразу;
- через 10 минут;
- через 1 час.

Практический вывод:

- endpoint уведомлений обязан быть идемпотентным;
- повторная доставка считается нормальным сценарием;
- повторное пополнение баланса по одному и тому же `operation_id` запрещено.

Источник:

- https://yoomoney.ru/docs/payment-buttons/using-api/notifications

### 6.2. Поля уведомления, которые нужны нам

- `notification_type` — тип уведомления;
- `operation_id` — идентификатор операции в истории кошелька;
- `amount` — сумма, зачисленная в кошелёк;
- `withdraw_amount` — сумма, списанная у отправителя;
- `currency` — код валюты, ожидается `643`;
- `datetime` — дата и время операции;
- `sender` — номер кошелька отправителя или пустая строка;
- `codepro` — сейчас всегда `false`;
- `label` — наша метка платежа;
- `sha1_hash` — подпись уведомления;
- `unaccepted` — сейчас всегда `false`, но поле приходит в контракте.

### 6.3. Какие значения особенно важны для ProxyAccessHub

- `label` — ищем локальную заявку на оплату;
- `operation_id` — защита от повторной обработки;
- `amount` — сумма фактического поступления;
- `currency` — fail-fast-проверка, что пришли рубли;
- `sha1_hash` — обязательная проверка подлинности уведомления.

## 7. Проверка `sha1_hash`

YooMoney требует обязательно проверять `sha1_hash`.

Строка для вычисления SHA-1 строится в таком порядке:

```text
notification_type&operation_id&amount&currency&datetime&sender&codepro&notification_secret&label
```

Где:

- `notification_secret` — секретное слово из настроек HTTP-уведомлений кошелька.

Порядок проверки:

1. Собрать строку именно в этом порядке.
2. Использовать UTF-8.
3. Посчитать SHA-1.
4. Перевести результат в HEX.
5. Сравнить с `sha1_hash` из уведомления.

Практический вывод для `ProxyAccessHub`:

- если `sha1_hash` не совпадает, запрос должен считаться недействительным;
- нельзя принимать уведомление только по `label`;
- нельзя выполнять бизнес-действия до проверки подписи.

Источник:

- https://yoomoney.ru/docs/payment-buttons/using-api/notifications

## 8. Как должен выглядеть endpoint уведомлений в ProxyAccessHub

Минимальные правила обработки:

1. Принять `POST` в `application/x-www-form-urlencoded`.
2. Прочитать все поля уведомления.
3. Проверить `sha1_hash`.
4. Проверить, что `currency == 643`.
5. Найти локальную заявку по `label`.
6. Проверить, что заявка ещё не обработана.
7. Проверить, что `operation_id` ещё не применялся.
8. Сохранить входящий платёж.
9. Пополнить баланс пользователя на сумму `amount`.
10. Запустить логику продления.
11. Вернуть `HTTP 200 OK`.

Отдельно важно:

- если подпись неверная, не принимать уведомление;
- если заявка не найдена, это кейс для ручной обработки;
- если платёж уже обработан, нужно ответить безопасно и не проводить повторные действия.

## 9. Ограничение по адресу HTTP-уведомлений

По документации HTTP-уведомления можно получать только на один адрес сервера для одного кошелька.

Практический вывод для `ProxyAccessHub`:

- если один кошелёк YooMoney обслуживает только один `ProxyAccessHub`, проблем нет;
- если один кошелёк используется несколькими сайтами, обязательно нужно жёстко кодировать происхождение в `label`;
- для нашего проекта лучше считать `label` обязательным и уникальным для каждой заявки.

Источник:

- https://yoomoney.ru/docs/payment-buttons/using-api/notifications

## 10. OAuth wallet API: когда он нам нужен

Для базового сценария оплаты через форму и HTTP-уведомления OAuth wallet API не обязателен.

Но он полезен для:

- ручной сверки спорных платежей;
- диагностики, если уведомление не пришло;
- просмотра входящих операций по кошельку;
- проверки состояния самого кошелька.

То есть для MVP `ProxyAccessHub` OAuth wallet API — это не часть основного happy-path, а инструмент надёжности и ручной поддержки.

## 11. Регистрация OAuth-приложения

Если мы хотим использовать wallet API, нужно зарегистрировать приложение в YooMoney.

При регистрации указываются:

- название приложения;
- адрес сайта;
- email для связи;
- `Redirect URI`;
- при необходимости `client_secret`.

После этого YooMoney выдаёт:

- `client_id`;
- при включённой проверке подлинности — `client_secret`.

Практический вывод:

- `client_id` и особенно `client_secret` должны храниться как секреты;
- этот сценарий нужен только если мы действительно включаем wallet API для сверки операций.

Источник:

- https://yoomoney.ru/docs/wallet/using-api/authorization/register-client

## 12. Получение OAuth-токена

После OAuth-авторизации временный код меняется на `access_token` запросом:

- `POST /oauth/token`

Основные параметры:

- `code`;
- `client_id`;
- `grant_type=authorization_code`;
- `redirect_uri`;
- при необходимости `client_secret`.

В ответ приходит:

- `access_token` при успехе;
- `error` при ошибке.

По документации токены, полученные позже 7 февраля 2018 года, действуют 3 года.

Практический вывод:

- токен wallet API — долгоживущий секрет;
- его нельзя хранить в открытом виде;
- нужен только если мы реализуем manual reconciliation или диагностику через wallet API.

Источник:

- https://yoomoney.ru/docs/wallet/using-api/authorization/obtain-access-token

## 13. Права токена, которые нам нужны

Для `ProxyAccessHub` достаточно только этих прав:

- `account-info`;
- `operation-history`.

Право `operation-details` может пригодиться позже, но для первой версии оно не обязательно.

Практический вывод:

- не нужно запрашивать платёжные права (`payment`, `payment-shop`, `payment-p2p`), потому что мы не инициируем wallet-платёж через OAuth API;
- минимальный безопасный scope для ручной сверки: `account-info operation-history`.

Источник:

- https://yoomoney.ru/docs/wallet/using-api/authorization/protocol-rights

## 14. Метод `account-info`

### 14.1. Назначение

Метод возвращает состояние кошелька пользователя.

Запрос:

- `POST /api/account-info`
- `Authorization: Bearer <access_token>`

Требуемое право:

- `account-info`

### 14.2. Что возвращает

Для нас наиболее важны:

- `account` — номер счёта;
- `balance` — баланс;
- `currency` — валюта, ожидается `643`;
- `account_status`;
- `account_type`;
- `balance_details.available` — доступный остаток, если блок `balance_details` присутствует.

### 14.3. Зачем это нужно ProxyAccessHub

- проверить, что приложение смотрит в нужный кошелёк;
- проверить, что кошелёк вообще жив и доступен;
- использовать как диагностический endpoint в админке или при ручной поддержке.

Источник:

- https://yoomoney.ru/docs/wallet/user-account/account-info

## 15. Метод `operation-history`

### 15.1. Назначение

Метод позволяет просматривать историю операций кошелька в постраничном режиме.

Запрос:

- `POST /api/operation-history`
- `Authorization: Bearer <access_token>`

Требуемое право:

- `operation-history`

### 15.2. Полезные параметры запроса

- `type=deposition` — только входящие пополнения;
- `records` — количество записей;
- `start_record` — пагинация;
- `details=true` — только если дополнительно есть право `operation-details`;
- интервал времени через `from` и `till`;
- отбор по `label`.

### 15.3. Поля ответа, которые важны нам

- `operation_id`;
- `status`;
- `datetime`;
- `amount`;
- `label`;
- `title`.

### 15.4. Зачем это нужно ProxyAccessHub

- ручная сверка спорного перевода, если уведомление не дошло;
- поиск поступления по `label`;
- поиск поступления по `operation_id`;
- помощь в разборе инцидентов и повторных попыток доставки уведомлений.

### 15.5. Практический вывод

Если основной поток строится на HTTP-уведомлениях, `operation-history` нужен как fallback для ручной поддержки, а не как обязательная часть основного автоматического сценария.

Источник:

- https://yoomoney.ru/docs/wallet/user-account/operation-history

## 16. Рекомендуемый набор конфигурации для ProxyAccessHub

Для формы и уведомлений:

- `Receiver` — номер кошелька;
- `NotificationSecret` — секрет проверки уведомлений;
- `SuccessUrl` — URL возврата после оплаты;
- `NotificationUrl` — публичный URL endpoint уведомлений.

Для дополнительной ручной сверки через wallet API:

- `ClientId`;
- `ClientSecret`, если используется;
- `AccessToken`;
- `Scope = account-info operation-history`.

## 17. Что закладывать в архитектуру ProxyAccessHub

### 17.1. Для MVP

- локальная сущность заявки на оплату;
- генерация уникального `label`;
- собственная HTML-форма `POST https://yoomoney.ru/quickpay/confirm`;
- endpoint HTTP-уведомлений;
- проверка `sha1_hash`;
- защита от повторной обработки по `operation_id`;
- пополнение баланса пользователя по `amount`;
- запуск логики продления после подтверждённого уведомления.

### 17.2. Для следующего уровня надёжности

- отдельный сервис ручной сверки через `operation-history`;
- диагностический вызов `account-info`;
- сохранение связи `paymentRequest -> label -> operation_id`.

## 18. Краткий итог по нужным API

Для `ProxyAccessHub` реально нужны:

1. Нестандартная форма перевода:
   - `POST https://yoomoney.ru/quickpay/confirm`
2. HTTP-уведомления:
   - входящий `POST` от YooMoney;
   - проверка `sha1_hash`;
   - обработка `label` и `operation_id`
3. Дополнительно для ручной поддержки:
   - `POST /api/account-info`
   - `POST /api/operation-history`

Этого достаточно, чтобы закрыть сценарий:

- создать заявку на оплату;
- отправить пользователя на оплату;
- принять уведомление;
- найти заявку;
- зачислить деньги;
- продолжить продление подписки.
