# Розгортання в Azure і налаштування workflow (Робота 6)

Схема: `api.yml` після кожного merge у `main` запускає тести (разом з наскрізним), застосовує міграції через **efbundle** і розгортає API в **Azure App Service**; `client.yml` розгортає клієнт у **Azure Static Web Apps**. У Azure workflow входить через **OpenID Connect**: пароль у GitHub не зберігається.

## 1. Ресурси Azure (один раз)

| Ресурс | Налаштування |
|---|---|
| Resource group | наприклад, `brokeboys-rg` |
| Azure Database for PostgreSQL – Flexible Server | база `catalogue`; публічний доступ увімкнено (workflow сам на час міграцій додає правило фаєрвола для свого IP) |
| App Service | Linux, стек **.NET 9**; **Configuration → General settings → Web sockets: On** (без цього SignalR працюватиме лише через довгі запити); бажано **Always on** |
| Application Insights | workspace-based; скопіюйте **Connection string** |
| Static Web App | план Free; джерело розгортання **Other** (розгортає workflow) |

Також дозвольте App Service дістатися бази: у PostgreSQL → **Networking** → «Allow public access from any Azure service within Azure…» (або додайте вихідні IP App Service).

## 2. App settings в App Service (Settings → Environment variables)

| Ім'я | Значення |
|---|---|
| `ConnectionStrings__Catalogue` | `Host=<сервер>.postgres.database.azure.com;Port=5432;Database=catalogue;Username=<користувач>;Password=<пароль>;Ssl Mode=Require` |
| `AzureAd__TenantId` | tenant ID |
| `AzureAd__ClientId` | Application (client) ID **API** |
| `Cors__ClientOrigin` | адреса Static Web App без слеша в кінці, наприклад `https://xxx.azurestaticapps.net` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | connection string Application Insights |

Рядок підключення **застосунку** лежить лише тут. Рядок для **міграцій** (див. нижче) — окремо, у secret GitHub.

## 3. Вхід workflow в Azure без пароля (OpenID Connect)

1. **Entra ID → App registrations → New registration**: `BrokeBoys GitHub Deploy` (single tenant). Скопіюйте Application (client) ID і tenant ID.
2. У ній: **Certificates & secrets → Federated credentials → Add credential**:
   - Scenario: **GitHub Actions deploying Azure resources**
   - Organization: `Maksumm`, Repository: `Programing_Web_Comand_Project_BrokeBoys`
   - Entity type: **Environment**, Environment name: `production`
   - Name: `github-production`. Subject буде `repo:Maksumm/Programing_Web_Comand_Project_BrokeBoys:environment:production`.
3. **Resource group → Access control (IAM) → Add role assignment → Contributor** → призначте застосунку `BrokeBoys GitHub Deploy`.

## 4. GitHub: environment `production`

**Settings → Environments → New environment → `production`**, далі:

**Secrets**

| Ім'я | Значення |
|---|---|
| `AZURE_CLIENT_ID` | client ID застосунку `BrokeBoys GitHub Deploy` |
| `AZURE_TENANT_ID` | tenant ID |
| `AZURE_SUBSCRIPTION_ID` | ID підписки Azure |
| `MIGRATIONS_CONNECTION_STRING` | рядок підключення до бази для **міграцій** (той самий формат, що вище; користувач із правом змінювати схему) |

**Variables**

| Ім'я | Значення |
|---|---|
| `AZURE_RESOURCE_GROUP` | назва resource group |
| `AZURE_POSTGRES_SERVER` | назва сервера PostgreSQL (без домену) |
| `AZURE_API_APP_NAME` | назва App Service |
| `AZURE_STATIC_WEB_APP_NAME` | назва Static Web App |
| `API_BASE_URL` | адреса API **зі слешем у кінці**, наприклад `https://brokeboys-api.azurewebsites.net/` — workflow записує її в `appsettings.Production.json` клієнта |
| `API_SCOPE` | `api://<API client ID>/access_as_user` |
| `CLIENT_ID` | Application (client) ID клієнта (SPA) |

## 5. Entra ID: адреса клієнта

У реєстрації **BrokeBoys Client** → Authentication → SPA → додайте Redirect URI `https://<адреса Static Web App>/authentication/login-callback`.

## 6. Перший запуск і перевірка

1. Зробіть merge у `main` → вкладка **Actions**: `API` (test → deploy) і `Client` (test → deploy) мають бути зеленими, у `API / test` — крок **End-to-end test**.
2. Відкрийте адресу Static Web App, увійдіть, відкрийте «Календар»: поруч з назвою товару має з'явитися позначка «Наживо» (клієнт підписаний на групу своєї категорії).
3. Змініть ціну через API (наприклад, `PUT /api/prices/7` з токеном адміністратора): ціна зміниться на відкритій сторінці без перезавантаження.

## 7. Знімок екрана Application Insights

1. Application Insights → **Investigate → Transaction search** → тип події **Request** → відкрийте запит `PUT /api/prices/{id}`.
2. У дереві трасування під запитом має бути span **BroadcastPriceChange** (теги `price.kind`, `product.id`, `category.ids`). Зробіть знімок екрана цього дерева.
3. Дані з'являються з затримкою 1–3 хвилини.

## 8. Наскрізний тест локально

```bash
dotnet build E2ETests
pwsh E2ETests/bin/Debug/net9.0/playwright.ps1 install chromium     # один раз; у Windows PowerShell: powershell -File ...
dotnet test E2ETests
```

Тест сам запускає PostgreSQL у контейнері (потрібен Docker), API на Kestrel, публікує клієнт (`dotnet publish`, перший раз — кілька хвилин) і відкриває його в Chromium. У цьому режимі клієнт замість входу Microsoft отримує тестовий токен (ключ `E2E:Token` у `appsettings.json`, який пише тест); API перевіряє цей токен так само, як справжній.
