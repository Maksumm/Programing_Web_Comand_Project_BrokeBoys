# Сезонні ціни — Blazor WebAssembly клієнт (команда BrokeBoys)

Blazor WebAssembly + MudBlazor. Інтерфейс українською та англійською (`.resx`, `IStringLocalizer<AppResources>`).
Тести: bUnit + xUnit (`BlazorClient.Tests`).

## Структура

`Layout`, `Pages`, `Components`, `Services`, `Models`, `Resources`.

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

Кожен екран має чотири стани: завантаження, помилка (з кнопкою повтору), порожній, з даними.

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

`ApiApp` — Minimal API; дані зберігаються в пам'яті процесу (`Data/CatalogueStore`, потокобезпечний singleton з демо-даними).
Кожен endpoint лежить в окремому файлі в теці своєї можливості `Features/<Можливість>/`, а `Map<Можливість>()` (`MapGroup`) підключає її до `Program.cs` одним викликом.

| Ресурс | Адреси | Відповіді |
|---|---|---|
| Категорії | `GET/POST /api/categories`, `GET/PUT/DELETE /api/categories/{id:int}` | 200, 201 + `Location`, 204, 400, 404, 409 (дубль назви; видалення категорії з товарами) |
| Товари | `/api/products` (фільтр `?categoryId=`) | 200, 201, 204, 400, 404, 409 |
| Ціни за місяць | `/api/prices` (фільтр `?productId=`) | 200, 201, 204, 400, 404, 409 (дубль «товар + місяць») |
| Підписки | `/api/subscriptions` (фільтр `?email=`) | 200, 201, 204, 400, 404, 409 (дубль «e-mail + категорія») |

- Кожна відповідь з помилкою має тіло `ProblemDetails` (`application/problem+json`); 400 — `ValidationProblemDetails` з ключами полів у camelCase.
- Повторний `PUT` знову дає 204, повторний `DELETE` — 404.
- Документ OpenAPI: `/openapi/v1.json` (лише в `Development`).
- CORS дозволено лише для адреси клієнта (`Cors:ClientOrigin` в `appsettings.json`, за замовчуванням `https://localhost:7223`); заголовок `Location` відкрито для читання.
- Кожен запит логується: метод, шлях, код і тривалість у мс (`RequestLoggingMiddleware`).

```bash
dotnet run --project ApiApp --launch-profile https   # https://localhost:7253
dotnet test ApiApp.Tests
```

## Запуск

```bash
dotnet run --project BlazorClient
dotnet test
```
