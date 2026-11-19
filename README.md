# Сезонні ціни — Blazor WebAssembly клієнт (команда BrokeBoys)

Blazor WebAssembly + MudBlazor. Інтерфейс українською та англійською (`.resx`, `IStringLocalizer<AppResources>`).
Тести: bUnit + xUnit (`BlazorClient.Tests`).

## Структура

`Layout`, `Pages`, `Components`, `Services`, `Models`, `Resources`.

- `Services/ICatalogueApi` / `CatalogueApi` — типізований HTTP-клієнт до `ApiApp`: читання (категорії, календар, підписки) і запис підписок (створення, перемикач активності, видалення). Адреса API — `ApiBaseUrl` у `wwwroot/appsettings.json` (за замовчуванням `https://localhost:7253/`). Помилки сервера перетворюються на локалізовані повідомлення на екрані; недоступний сервер дає стан «помилка» з кнопкою повтору.
- `Services/ListState<T>` — scoped-сервіс зі станом екрана (`Loading` / `Error` / `Empty` / `Success`) і подією `Changed`.
- `Services/CultureService` — вибір мови: зберігає її в `localStorage`, застосовує при старті (`Program.cs`).
- `Components/CategoryCard`, `SubscriptionRow` — дочірні компоненти з `[Parameter]` і `EventCallback`.
- `Components/SubscribeFormComponent` — форма з DataAnnotations, `EditForm` і MudBlazor-полями з `For`.

## Таблиця екранів

| Екран | Маршрут | Компоненти, що перерендерюються після зміни стану | Стан |
|---|---|---|---|
| Календар сезонних цін | `/`, `/calendar` | `SeasonalCalendarPage` (12 стовпчиків місяців, `@key`) | `ListState<SeasonalPrice>` |
| Категорії | `/categories` | `CategoriesPage` → `CategoryCard` (`@key`, `OnSelected`) | `ListState<Category>` |
| Мої підписки | `/subscriptions` | `SubscriptionsPage` → `SubscriptionRow` (`@key`, `OnToggled`), `SubscribeFormComponent` (`OnSubmitted`) | `ListState<Subscription>` |
| Керування каталогом (лише `Admin`) | `/admin` | `AdminPage` → `AdminCategoriesPanel`, `AdminProductsPanel`, `AdminPricesPanel` (форма додавання/зміни, видалення з підтвердженням, `@key`) | власний стан панелі, `IAdminApi` |

Кожен екран має чотири стани: завантаження, помилка (з кнопкою повтору), порожній, з даними.

### Оновлення наживо та сповіщення

Хаб `/hubs/prices` розсилає повідомлення лише групам категорій, у яких зникла чи змінилася відповідна подія (`Clients.Groups`):

| Повідомлення | Коли | Поля |
|---|---|---|
| `PriceChanged` | ціну створено, змінено чи видалено | `Kind`, `CategoryId`, `ProductId`, `Month`, `Price`, `ProductNameUk`, `ProductNameEn` |
| `CatalogueChanged` | товар створено, змінено (також перенесено до іншої категорії) чи видалено; категорію змінено чи видалено | `Entity` (`Product`/`Category`), `Kind`, `CategoryId`, `ProductId`, `NameUk`, `NameEn` |

Повторний запит із тими самими значеннями нічого не розсилає. Кожна розсилка — окремий span у трасі (`BroadcastPriceChange`, `BroadcastCatalogueChange`).

Клієнт: календар оновлює вибірку товарів і ціни показаного товару (а якщо його видалили, показує інший). Компонент `LiveToasts` у верхній панелі
приєднується до груп категорій з активних підписок і показує тост із назвою товару мовою інтерфейсу («Дрова, дуб: ціна на липень тепер 1250»),
про зміну товару чи категорії — лише для категорій, на які користувач підписаний.

### Екрани адміністратора

Сторінка `/admin` доступна лише ролі `Admin` (посилання в верхній панелі теж видиме лише їй; сервер усе одно перевіряє роль політикою
`ManageCatalogue`). Три вкладки працюють з наявними endpoint-ами API: категорії (`/api/categories`), товари (`/api/products`) і
ціни за місяцями (`/api/prices`). Після збереження чи видалення список оновлюється з сервера; помилки сервера показуються
причиною (дублікат або пов'язані дані — 409, невалідні поля — 400, немає ролі — 403). Зміна ціни автоматично розсилається
SignalR-хабом підписаним на категорію клієнтам.

## Один екран двома мовами та поле для обох культур

Екран «Мої підписки», поле «Поріг» (`decimal?`, `MudNumericField` з `Culture = CurrentCulture`):

| Культура | Введення | Значення в моделі | Відображення в рядку |
|---|---|---|---|
| `uk-UA` | `3,5` | `3.5` | `… • поріг: індекс ≤ 3,5` |
| `en-US` | `3.5` | `3.5` | `… • threshold: index ≤ 3.5` |

Перевіряється тестами `SubscribeFormTests.Threshold_ParsesFractionalNumber_ForCulture`
та `SubscriptionsPageTests.Threshold_IsShownInRow_WithCultureSpecificSeparator`.

## Робочий процес

- Пряме надсилання в `main` заборонено (branch protection), кожне завдання — окрема гілка та PR з review.
- Тест комітиться окремим комітом раніше за код, з яким він проходить.

## Сервер: ASP.NET Core Web API (робота 3)

`ApiApp` — Minimal API; дані зберігаються в PostgreSQL через EF Core (`Data/AppDbContext`, схема й ER-діаграма — `docs/er-diagram.md`).
Кожен endpoint лежить в окремому файлі в теці своєї можливості `Features/<Можливість>/`, а `Map<Можливість>()` (`MapGroup`) підключає її до `Program.cs` одним викликом.

| Ресурс | Адреси | Відповіді |
|---|---|---|
| Категорії | `GET/POST /api/categories`, `GET/PUT/DELETE /api/categories/{id:int}` | 200, 201 + `Location`, 204, 400, 404, 409 (дубль назви; видалення категорії з товарами) |
| Товари | `/api/products` (фільтр `?categoryId=`) | 200, 201, 204, 400, 404, 409 |
| Ціни за місяць | `/api/prices` (фільтр `?productId=`) | 200, 201, 204, 400, 404, 409 (дубль «товар + місяць») |
| Підписки (лише власні, за `oid` з токена) | `/api/subscriptions` (фільтр `?email=`) | 200, 201, 204, 400, 404 (також чужа підписка), 409 (дубль «користувач + категорія») |

- Кожна відповідь з помилкою має тіло `ProblemDetails` (`application/problem+json`); 400 — `ValidationProblemDetails` з ключами полів у camelCase.
- Повторний `PUT` знову дає 204, повторний `DELETE` — 404.
- Документ OpenAPI: `/openapi/v1.json` (лише в `Development`).
- CORS дозволено лише для адреси клієнта (`Cors:ClientOrigin` в `appsettings.json`, за замовчуванням `https://localhost:7223`); заголовок `Location` відкрито для читання.
- Усі endpoint-и захищені токеном Microsoft Entra ID: читання вимагає scope `access_as_user`, зміна каталогу — ще й роль `Admin`, підписки — роль `Subscriber`; без токена 401, без прав 403. Таблиця — `docs/endpoints-security.md`, налаштування порталу — `docs/entra-setup.md`; `GET /api/me` повертає ім'я, пошту й ролі користувача.
- Оновлення «наживо»: SignalR hub `/hubs/prices`; клієнт приєднується до групи категорії показаного товару, а після кожної збереженої зміни ціни (створення, заміна, видалення) сервер надсилає повідомлення лише групам її категорії. Розсилка виконується в окремому span (`BroadcastPriceChange`) і видна в Application Insights; логування — лише через `[LoggerMessage]`.
- Кожен запит логується: метод, шлях, код і тривалість у мс (`RequestLoggingMiddleware`).

Перший запуск бази (потрібен Docker і `dotnet-ef`):

```bash
docker compose up -d
dotnet user-secrets set "ConnectionStrings:Catalogue" "Host=localhost;Port=5433;Database=catalogue;Username=postgres;Password=postgres" --project ApiApp
dotnet tool install --global dotnet-ef
dotnet ef database update --project ApiApp            # застосовує міграції (схему змінюють лише вони)
dotnet run --project ApiApp --launch-profile https    # https://localhost:7253; в Development наповнює порожню базу демо-даними
dotnet test ApiApp.Tests                              # Testcontainers сам запускає postgres:18-alpine — Docker має працювати
```

Читання виконується асинхронно з `AsNoTracking()`; пов'язані записи підписок завантажуються одним запитом через `Include`.

## Вхід через Microsoft Entra ID (робота 5)
 
- Налаштування Entra ID, ролі й знімки екрана: [docs/entra-setup.md](docs/entra-setup.md)
- Таблиця endpoints (потрібні scope і роль, коди відповідей): [docs/endpoints-security.md](docs/endpoints-security.md)
- Тести авторизації (401/403/успіх): `ApiApp.Tests/AuthorizationTests.cs`
 
## Запуск

Спершу запустіть API, потім клієнт (у двох терміналах):

```bash
dotnet run --project ApiApp --launch-profile https      # https://localhost:7253
dotnet run --project BlazorClient --launch-profile https # https://localhost:7223
dotnet test
```

Якщо браузер скаржиться на сертифікат: `dotnet dev-certs https --trust`.
Якщо екрани показують помилку замість даних — див. `docs/troubleshooting.md` (на екрані видно причину та код HTTP).

## Робота 6: результат

**Застосунок:** https://zealous-wave-08945d810.6.azurestaticapps.net

**Успішні запуски workflow (merge у `main`, e2e-тест пройдено):**
- API: https://github.com/Maksumm/Programing_Web_Comand_Project_BrokeBoys/actions/runs/43203129359/job/131131399267
- Client: https://github.com/Maksumm/Programing_Web_Comand_Project_BrokeBoys/actions/runs/43203129303

### Trace зміни ціни в Application Insights (span BroadcastPriceChange)
(<img width="1280" height="615" alt="Знімок екрана 2026-11-19 220413" src="https://github.com/user-attachments/assets/cc97d8b1-a964-4a43-8a5a-5701fd64170f" />

### App settings в App Service
<img width="1280" height="620" alt="Знімок екрана 2026-11-19 214958" src="https://github.com/user-attachments/assets/bbe9097e-91ab-42ad-bc98-e481d060f171" />


### Secrets і variables environment production
<img width="1280" height="614" alt="Знімок екрана 2026-11-19 221113" src="https://github.com/user-attachments/assets/34ffe538-0919-4e7f-ab8a-c81bb0310a8c" />


### Крок End-to-end test в api.yml
<img width="1280" height="616" alt="Знімок екрана 2026-11-19 220640" src="https://github.com/user-attachments/assets/5f356534-15ea-443d-a06f-93d3b85551f7" />

