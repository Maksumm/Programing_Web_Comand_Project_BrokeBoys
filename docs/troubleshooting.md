# Що означає помилка на екрані

Екрани «Календар», «Категорії» й «Підписки» показують **причину** збою читання та, якщо сервер відповів, код HTTP
(наприклад, `HTTP 401`). Той самий запит видно в браузері: F12 → Network → червоний рядок → вкладка Headers/Response.

| Що показано | Що це означає | Що зробити |
|---|---|---|
| «Не вдалося з'єднатися з API…» (без коду HTTP) | API не запущено, неправильна адреса `ApiBaseUrl` або браузер заблокував запит (CORS) | `dotnet run --project ApiApp --launch-profile https`; відкрийте `https://localhost:7253/openapi/v1.json` — має з'явитися JSON. `Cors:ClientOrigin` має дорівнювати адресі клієнта (`https://localhost:7223`) |
| «Сеанс завершився. Увійдіть знову.» + `HTTP 401` | API відхилив токен: чужий audience/issuer або прострочений | Перевірте `AzureAd:TenantId` і `AzureAd:ClientId` у user-secrets API та `ApiScope` клієнта (`api://<API client ID>/access_as_user`); вийдіть і увійдіть знову |
| «Недостатньо прав…» + `HTTP 403` | У токені немає scope `access_as_user` | Entra ID → реєстрація клієнта → API permissions → **Grant admin consent**; вийдіть і увійдіть знову |
| «Сервер не зміг обробити запит…» + `HTTP 500` | Зазвичай база даних: не запущена або без міграцій | `docker compose up -d`, `dotnet ef database update --project ApiApp`, перезапустіть API; деталі — у консолі API |
| «API повернуло неочікувану відповідь…» | Замість JSON прийшла HTML-сторінка: `ApiBaseUrl` вказує на сам клієнт | Виправте `ApiBaseUrl` у `BlazorClient/wwwroot/appsettings.json` (за замовчуванням `https://localhost:7253/`) |

## «Для цієї сторінки потрібна роль Subscriber»

Клієнт дізнається ролі із `GET /api/me`. Якщо цей запит не вдався (див. таблицю вище), роль здається відсутньою.
Коли API працює, а повідомлення лишається: Entra ID → Enterprise applications → **BrokeBoys API** → Users and groups →
призначте собі роль `Subscriber` (і `Admin`, щоб змінювати каталог), потім вийдіть і увійдіть знову, щоб отримати новий токен.
