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

## Запуск

```bash
dotnet run --project BlazorClient
dotnet test
```
