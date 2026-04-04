# Telemt API для ProxyAccessHub

## 1. Источник сведений

Этот файл составлен по исходникам `telemt-main`, в первую очередь по:

- `docs/API.md`
- `src/api/mod.rs`
- `src/api/users.rs`
- `tools/telemt_api.py`

Ниже зафиксирован текущий контракт API и нюансы, которые важно учесть при проектировании `ProxyAccessHub`.

## 2. Общие свойства API

- API работает по HTTP/1.1.
- Базовый префикс маршрутов: `/v1`.
- Формат успешного ответа: JSON-обёртка с `ok`, `data`, `revision`.
- Формат ошибки: JSON-обёртка с `ok = false`, `error`, `request_id`.
- Для мутаций поддерживается optimistic concurrency через заголовок `If-Match`.
- `revision` — это SHA-256 хэш текущего `config.toml`.

### 2.1. Базовый успешный ответ

```json
{
  "ok": true,
  "data": {},
  "revision": "sha256-hex"
}
```

### 2.2. Базовый ответ с ошибкой

```json
{
  "ok": false,
  "error": {
    "code": "machine_code",
    "message": "human-readable"
  },
  "request_id": 1
}
```

## 3. Настройки API в telemt

API управляется через секцию `[server.api]`.

Ключевые параметры:

- `enabled` — включает API.
- `listen` — адрес прослушивания, например `127.0.0.1:9091`.
- `whitelist` — список CIDR, которым разрешён доступ.
- `auth_header` — точное ожидаемое значение заголовка `Authorization`.
- `request_body_limit_bytes` — лимит тела запроса.
- `minimal_runtime_enabled` — включает часть runtime endpoints.
- `runtime_edge_enabled` — включает edge/runtime endpoints.
- `read_only` — запрещает изменяющие операции.

## 4. Порядок обработки запроса

Telemt обрабатывает входящий запрос в таком порядке:

1. Проверка, включён ли API.
2. Проверка source IP по whitelist.
3. Проверка точного совпадения `Authorization` с `auth_header`, если он задан.
4. Сопоставление маршрута и HTTP-метода.
5. Проверка `read_only` для изменяющих операций.
6. Чтение и разбор JSON.
7. Бизнес-валидация и запись конфига.

Для `ProxyAccessHub` это означает:

- если доступ идёт не с разрешённого IP, будет `403`;
- если `Authorization` не совпадает, будет `401`;
- если API включён в `read_only`, создавать и изменять пользователей нельзя;
- query string не влияет на matching маршрута.

## 5. Все доступные endpoint'ы

## 5.1. Базовые и системные

- `GET /v1/health`
- `GET /v1/system/info`
- `GET /v1/runtime/gates`
- `GET /v1/runtime/initialization`
- `GET /v1/limits/effective`
- `GET /v1/security/posture`
- `GET /v1/security/whitelist`

Назначение:

- проверка живости API;
- получение версии, uptime и пути к активному конфигу;
- просмотр runtime-состояния;
- просмотр effective limits и security posture.

## 5.2. Статистика и runtime-снимки

- `GET /v1/stats/summary`
- `GET /v1/stats/zero/all`
- `GET /v1/stats/upstreams`
- `GET /v1/stats/minimal/all`
- `GET /v1/stats/me-writers`
- `GET /v1/stats/dcs`
- `GET /v1/runtime/me_pool_state`
- `GET /v1/runtime/me_quality`
- `GET /v1/runtime/upstream_quality`
- `GET /v1/runtime/nat_stun`
- `GET /v1/runtime/me-selftest`
- `GET /v1/runtime/connections/summary`
- `GET /v1/runtime/events/recent`

Это диагностические и мониторинговые маршруты.

Для `ProxyAccessHub` они полезны как минимум для:

- проверки состояния сервера;
- отображения health в админке;
- просмотра количества активных соединений;
- оценки нагрузки по пользователям;
- диагностики проблемных серверов.

## 5.3. Пользователи

- `GET /v1/stats/users`
- `GET /v1/users`
- `POST /v1/users`
- `GET /v1/users/{username}`
- `PATCH /v1/users/{username}`
- `DELETE /v1/users/{username}`
- `POST /v1/users/{username}/rotate-secret`

Важно: маршрут `POST /v1/users/{username}/rotate-secret` описан в контракте, но в текущем релизе фактически возвращает `404 not_found`.

## 6. Команды, которые реально нужны ProxyAccessHub

Для текущего проекта практически значимы прежде всего эти операции:

- `GET /v1/users`
- `GET /v1/users/{username}`
- `POST /v1/users`
- `PATCH /v1/users/{username}`
- `DELETE /v1/users/{username}`
- `GET /v1/stats/summary`
- `GET /v1/runtime/connections/summary`

Предлагаемое назначение:

- `POST /v1/users` — создание нового пользователя после успешной оплаты.
- `GET /v1/users` — синхронизация пользователей с сервером.
- `GET /v1/users/{username}` — получение информации по конкретному пользователю.
- `PATCH /v1/users/{username}` — продление срока, изменение лимитов, квот и других параметров.
- `DELETE /v1/users/{username}` — удаление пользователя по решению администратора.
- `GET /v1/stats/summary` — лёгкая проверка доступности и общего состояния.
- `GET /v1/runtime/connections/summary` — расширенная диагностика активности.

## 7. Контракты по пользователям

## 7.1. Создание пользователя

Маршрут:

- `POST /v1/users`

Тело запроса:

```json
{
  "username": "alice",
  "secret": "32hexoptional",
  "user_ad_tag": "32hexoptional",
  "max_tcp_conns": 10,
  "expiration_rfc3339": "2026-12-31T23:59:59Z",
  "data_quota_bytes": 1000000000,
  "max_unique_ips": 3
}
```

Поля:

- `username` — обязателен, допустимы `[A-Za-z0-9_.-]`, длина `1..64`;
- `secret` — необязателен, если не передан, сервер сгенерирует сам;
- `user_ad_tag` — необязателен, строго `32` hex-символа;
- `max_tcp_conns` — необязателен;
- `expiration_rfc3339` — необязателен;
- `data_quota_bytes` — необязателен;
- `max_unique_ips` — необязателен.

Успешный ответ:

- HTTP `201`
- `data.user` — объект пользователя
- `data.secret` — итоговый секрет пользователя

Для `ProxyAccessHub` это важно:

- username в telemt допускает символы `_ . -`, а не только буквы и цифры;
- если хотим хранить собственный идентификатор пользователя, его формат должен соответствовать этому правилу;
- если secret не передавать, telemt сам его создаст и вернёт в ответе.

## 7.2. Получение списка пользователей

Маршруты:

- `GET /v1/stats/users`
- `GET /v1/users`

Оба маршрута возвращают массив `UserInfo[]`.

В объекте пользователя есть:

- `username`
- `user_ad_tag`
- `max_tcp_conns`
- `expiration_rfc3339`
- `data_quota_bytes`
- `max_unique_ips`
- `current_connections`
- `active_unique_ips`
- `active_unique_ips_list`
- `recent_unique_ips`
- `recent_unique_ips_list`
- `total_octets`
- `links`

Поле `links` уже содержит готовые `tg://proxy` ссылки:

- `classic`
- `secure`
- `tls`

Это очень важно для `ProxyAccessHub`, потому что:

- после создания пользователя telemt уже может вернуть готовую ссылку;
- при синхронизации можно подтягивать актуальные ссылки с сервера;
- можно искать пользователя не только по `username`, но и по прокси-ссылкам, если это понадобится в локальной логике.

## 7.3. Получение одного пользователя

Маршрут:

- `GET /v1/users/{username}`

Возвращает один объект `UserInfo`.

Если пользователь не найден:

- HTTP `404`
- `error.code = not_found`

## 7.4. Частичное изменение пользователя

Маршрут:

- `PATCH /v1/users/{username}`

Допустимые поля:

- `secret`
- `user_ad_tag`
- `max_tcp_conns`
- `expiration_rfc3339`
- `data_quota_bytes`
- `max_unique_ips`

Особенности:

- изменяются только переданные поля;
- отсутствующие поля остаются без изменений;
- явного механизма “очистить поле в null” контракт не даёт;
- при успешной операции возвращается обновлённый `UserInfo`.

Для `ProxyAccessHub` это основной маршрут для продления доступа:

- менять `expiration_rfc3339`;
- при необходимости обновлять сетевые лимиты;
- при необходимости обновлять квоты.

## 7.5. Удаление пользователя

Маршрут:

- `DELETE /v1/users/{username}`

Успешный ответ:

- HTTP `200`
- в `data` возвращается имя удалённого пользователя

Важный нюанс:

- нельзя удалить последнего пользователя в конфиге;
- в этом случае сервер вернёт `409 last_user_forbidden`.

## 7.6. Ротация секрета

Маршрут:

- `POST /v1/users/{username}/rotate-secret`

Контракт тела существует:

```json
{
  "secret": "32hexoptional"
}
```

Но в текущем релизе маршрут недоступен и возвращает `404 not_found`.

Для `ProxyAccessHub` это означает:

- на этот маршрут нельзя опираться как на рабочую бизнес-функцию;
- если понадобится смена секрета, лучше использовать `PATCH /v1/users/{username}` с новым `secret`.

## 8. Важные особенности маршрутизации

- Сопоставление пути строгое, без нормализации.
- Trailing slash не нормализуется.
- `/v1/users/` вернёт `404`.
- `/v1/users/{username}/...` с лишними сегментами не сработает.
- `PUT /v1/users/{username}` вернёт `405`.
- `POST /v1/users/{username}` вернёт `404`.
- Query string не влияет на matching маршрута.

Для `ProxyAccessHub` это означает, что URL надо формировать строго без лишнего `/`.

## 9. Важные особенности аутентификации и сетевого доступа

### 9.1. Whitelist

Whitelist проверяется по реальному IP TCP-соединения.

Важно:

- `X-Forwarded-For` не учитывается;
- если telemt стоит за reverse proxy, whitelist должен учитывать IP прокси, а не клиента браузера;
- для интеграции `ProxyAccessHub -> telemt` нужно либо размещать `ProxyAccessHub` в разрешённой сети, либо настроить whitelist соответствующим образом.

### 9.2. Authorization

- Проверка заголовка `Authorization` идёт по точному совпадению строки.
- Если в `auth_header` задано конкретное значение, его нужно передавать без изменений.
- Формат не навязывается, это не обязательно `Bearer ...`.

Для `ProxyAccessHub` это удобно: можно хранить готовое значение заголовка в конфиге и отправлять как есть.

## 10. Важные коды ошибок

Наиболее важные для интеграции:

- `400 bad_request` — неверный JSON или ошибка валидации;
- `401 unauthorized` — неверный или отсутствующий `Authorization`;
- `403 forbidden` — IP не проходит whitelist;
- `403 read_only` — telemt в режиме только чтения;
- `404 not_found` — неизвестный маршрут или пользователь не найден;
- `405 method_not_allowed` — неверный метод для существующего маршрута;
- `409 revision_conflict` — конфликт `If-Match`;
- `409 user_exists` — пользователь уже существует;
- `409 last_user_forbidden` — нельзя удалить последнего пользователя;
- `413 payload_too_large` — слишком большое тело;
- `500 internal_error` — внутренняя ошибка telemt;
- `503 api_disabled` — API отключён в конфиге.

## 11. `If-Match` и revision

Mutating endpoints поддерживают `If-Match`.

Это значит:

- можно читать текущий `revision`;
- передавать его в `If-Match` при создании, изменении и удалении;
- если конфиг поменялся между чтением и записью, telemt вернёт `409 revision_conflict`.

Для `ProxyAccessHub` это полезно, но не обязательно в первой версии.

Рекомендуемый подход:

- на первом этапе можно работать без `If-Match`, если нет параллельных независимых клиентов;
- позже имеет смысл включить `If-Match`, если админка, фоновые синхронизации и ручные операции начнут пересекаться.

## 12. Нюансы генерации ссылок

Telemt сам строит `tg://proxy` ссылки из конфига.

При генерации ссылок учитываются:

- `general.links.public_port`
- `general.links.public_host`
- `server.listeners`
- `announce`
- `announce_ip`
- автоматически определённые внешние IP
- включённые режимы `classic`, `secure`, `tls`

Для `ProxyAccessHub` это означает:

- не нужно самостоятельно собирать proxy-ссылку, если telemt уже вернул `links`;
- но нужно понимать, что итоговая ссылка зависит от конфигурации самого telemt-сервера;
- при изменении конфига telemt ссылка пользователя может измениться.

## 13. Полезные endpoint'ы для синхронизации и диагностики

Для фоновой синхронизации и админки особенно полезны:

- `GET /v1/users`
- `GET /v1/stats/summary`
- `GET /v1/runtime/connections/summary`
- `GET /v1/runtime/events/recent`
- `GET /v1/security/posture`

Возможное применение:

- подтягивать пользователей в локальную БД;
- сверять наличие и параметры пользователей;
- показывать health сервера в панели;
- отслеживать проблемы с API или runtime;
- отображать нагрузку по пользователям.

## 14. Практические выводы для ProxyAccessHub

### 14.1. Для создания нового пользователя

Использовать:

- `POST /v1/users`

Сохранять у себя:

- `username`
- `secret`, если нужен локально;
- `links`, вернувшиеся от telemt;
- дату создания;
- привязанный сервер;
- статус локальной синхронизации.

### 14.2. Для продления доступа

Использовать:

- `PATCH /v1/users/{username}`

Основное поле:

- `expiration_rfc3339`

### 14.3. Для сверки и поиска

Использовать:

- `GET /v1/users`
- `GET /v1/users/{username}`

### 14.4. Для ручного удаления

Использовать:

- `DELETE /v1/users/{username}`

### 14.5. Для смены секрета

Не использовать:

- `POST /v1/users/{username}/rotate-secret`

Вместо этого использовать:

- `PATCH /v1/users/{username}` с новым `secret`

## 15. Ограничения и риски для интеграции

- API не предоставляет встроенный webhook или callback.
- Всё взаимодействие строится по polling и командам со стороны `ProxyAccessHub`.
- Нет встроенной пагинации списка пользователей.
- Часть runtime endpoint'ов отключается флагами `minimal_runtime_enabled` и `runtime_edge_enabled`.
- Изменения в `server.api` считаются restart-required.
- При записи telemt обновляет конфиг на диске, поэтому любые ручные правки `config.toml` должны учитывать вероятность конфликтов.

## 16. Что стоит заложить в архитектуру ProxyAccessHub

- клиент `TelemtApiClient` с явной поддержкой `GET /v1/users`, `POST /v1/users`, `PATCH /v1/users/{username}`, `DELETE /v1/users/{username}`;
- хранение адреса API, `Authorization` и сетевых ограничений в конфиге сервера;
- локальную таблицу синхронизации пользователей;
- механизм ручной обработки для кейсов, когда оплата прошла, а `POST /v1/users` или `PATCH /v1/users/{username}` завершился ошибкой;
- фоновую сверку локальной БД с `GET /v1/users`;
- отдельную диагностику статуса сервера через `GET /v1/stats/summary` и смежные runtime endpoint'ы.
