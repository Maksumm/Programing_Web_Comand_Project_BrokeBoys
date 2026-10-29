# Захист API: які endpoint-и що вимагають

API перевіряє **access-токен Microsoft Entra ID** (`Authorization: Bearer …`): підпис, емітента, **audience** (токен має бути виданий саме для цього API), термін дії, а потім політики доступу.

| Позначення | Значення |
|---|---|
| scope | `access_as_user` — делегований дозвіл, який клієнт запитує від імені користувача (claim `scp`) |
| Admin / Subscriber | app roles з реєстрації API (claim `roles`) |

## Таблиця endpoint-ів

| Endpoint | Методи | Потрібний scope | Потрібна роль | Без токена | Токен без scope | Токен без ролі |
|---|---|---|---|---|---|---|
| `/api/me` | GET | `access_as_user` | — | 401 | 403 | — |
| `/api/categories`, `/api/categories/{id}` | GET | `access_as_user` | — | 401 | 403 | — |
| `/api/categories`, `/api/categories/{id}` | POST, PUT, DELETE | `access_as_user` | **Admin** | 401 | 403 | 403 |
| `/api/products`, `/api/products/{id}` | GET | `access_as_user` | — | 401 | 403 | — |
| `/api/products`, `/api/products/{id}` | POST, PUT, DELETE | `access_as_user` | **Admin** | 401 | 403 | 403 |
| `/api/prices`, `/api/prices/{id}` | GET | `access_as_user` | — | 401 | 403 | — |
| `/api/prices`, `/api/prices/{id}` | POST, PUT, DELETE | `access_as_user` | **Admin** | 401 | 403 | 403 |
| `/api/subscriptions`, `/api/subscriptions/{id}` | GET, POST, PUT, DELETE | `access_as_user` | **Subscriber** | 401 | 403 | 403 |

Підписки **персональні**: сервер бере власника з claim `oid` токена (або `sub`) і показує/змінює лише підписки цього користувача. Чужа підписка відповідає `404`, як відсутня. Токен без `oid` і `sub` отримує `403`.
| `/openapi/v1.json` (лише Development) | GET | — | — | відкритий | — | — |

## Інші випадки з кодом 401

Токен, виданий для **іншого API** (інший audience), **прострочений** токен і токен із **чужим підписом** також дають **401**. Усі відповіді 401 і 403 мають тіло `ProblemDetails` (`application/problem+json`).

Токен застосунку (client credentials) без користувача не має claim `scp`, тож отримує **403**.

## Де це в коді

| Що | Файл |
|---|---|
| перевірка токена, політики `Read`, `ManageCatalogue`, `ManageSubscriptions` | `ApiApp/Common/EntraAuthentication.cs` |
| назви політик, scope і ролей | `ApiApp/Common/Policies.cs` |
| `/api/me` (ім'я, пошта, ролі з токена) | `ApiApp/Features/Me/` |
| тести 401/403/успіху з роллю | `ApiApp.Tests/AuthorizationTests.cs` |

## Що перевіряє клієнт, а що сервер

Клієнт показує сторінку підписок лише користувачам з роллю `Subscriber` (ролі він дізнається з `/api/me`), але це лише зручність: **рішення завжди приймає API**.
