# Налаштування Microsoft Entra ID (Робота 5)

Усі кроки виконуються в порталі <https://entra.microsoft.com> облікового запису, що має право реєструвати застосунки. Потрібно **два** застосунки: API і клієнт.

> **Ролі.** Нижче використано `Admin` (керує категоріями, товарами й цінами) та `Subscriber` (керує підписками). Це пропозиція; якщо історії користувача вашого продукту вимагають інших ролей, змініть `ApiApp/Common/Policies.cs`, політики в `EntraAuthentication.cs`, таблицю `docs/endpoints-security.md` і тести.

## 1. Реєстрація API

1. **Entra ID → App registrations → New registration**. Name: `BrokeBoys API`. Supported account types: **Single tenant**. Redirect URI не потрібен. **Register**.
2. На сторінці **Overview** скопіюйте **Application (client) ID** (це `YOUR-API-CLIENT-ID`) і **Directory (tenant) ID** (`YOUR-TENANT-ID`).
3. **Expose an API → Application ID URI → Add → Save**: залиште `api://<API client ID>`.
4. **Expose an API → Add a scope**: Scope name `access_as_user`; Who can consent: **Admins and users**; заповніть назву й опис згоди (наприклад, «Доступ до API від імені користувача»); State: **Enabled** → **Add scope**.
5. **Manifest**: переконайтеся, що в розділі `api` є `"requestedAccessTokenVersion": 2` (токени v2). API приймає й v1, але v2 — рекомендований варіант.
6. **App roles → Create app role** двічі:

| Display name | Allowed member types | Value | Description |
|---|---|---|---|
| Admin | Users/Groups | `Admin` | Керує категоріями, товарами й цінами |
| Subscriber | Users/Groups | `Subscriber` | Керує підписками |

## 2. Призначення ролей учасникам команди

1. **Entra ID → Enterprise applications → BrokeBoys API → Users and groups → Add user/group**.
2. Оберіть користувача й роль (**Admin** або **Subscriber**) → **Assign**.
3. **Кожна роль має бути призначена щонайменше одному учаснику команди.** Один користувач може мати обидві ролі: додайте два призначення.

## 3. Реєстрація клієнта (SPA)

1. **App registrations → New registration**. Name: `BrokeBoys Client`; Single tenant.
2. Redirect URI: платформа **Single-page application (SPA)** (не «Web»), значення `https://localhost:7223/authentication/login-callback`. Після розгортання в Azure додайте ще й адресу Static Web Apps із тим самим шляхом `/authentication/login-callback`.
3. З **Overview** скопіюйте **Application (client) ID** (`YOUR-CLIENT-ID`).
4. **API permissions → Add a permission → My APIs → BrokeBoys API → Delegated permissions → `access_as_user` → Add permissions**.
5. Натисніть **Grant admin consent for …** (якщо ви адміністратор). Інакше кожен користувач підтвердить згоду під час першого входу.

## 4. Значення в репозиторії

Клієнт — `BlazorClient/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://localhost:7253/",
  "ApiScope": "api://<API client ID>/access_as_user",
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<tenant ID>",
    "ClientId": "<SPA client ID>",
    "ValidateAuthority": true
  }
}
```

API — user-secrets (ідентифікатори не є секретами, але залежать від вашого tenant):

```bash
dotnet user-secrets set "AzureAd:TenantId" "<tenant ID>" --project ApiApp
dotnet user-secrets set "AzureAd:ClientId" "<API client ID>" --project ApiApp
```

## 5. Перевірка

1. Запустіть API і клієнт (`dotnet run --project ApiApp --launch-profile https`, `dotnet run --project BlazorClient --launch-profile https`).
2. Відкрийте `https://localhost:7223/`: вас перенаправить на вхід Microsoft. Після входу у верхній панелі з'явиться **ваше ім'я** й кнопка **Вийти**.
3. Користувач з роллю **Subscriber** бачить сторінку «Підписки»; без ролі бачить повідомлення, що потрібна роль.
4. Щоб побачити вміст токена: у браузері скопіюйте access-токен із запиту до API (DevTools → Network → заголовок `Authorization`) і вставте на <https://jwt.ms>. Перевірте `aud` (`api://<API client ID>` або GUID), `scp` (`access_as_user`) і `roles`.

## 6. Знімок екрана для здачі

Після входу: у верхній панелі видно ім'я користувача, відкрита сторінка, доступна лише з потрібною роллю (наприклад, «Підписки» для `Subscriber`).
