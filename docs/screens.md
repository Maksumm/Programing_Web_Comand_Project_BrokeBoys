# Таблиця екранів

Усі застосунки — Blazor WebAssembly + MudBlazor. Тести — bUnit (`BlazorClient.Tests`).
Кожен рядок нижче підтверджується тестом із вказаної колонки.

| Екран | Маршрут | Компоненти, що перерендерюються після зміни стану | Той самий екран двома мовами (uk-UA / en-US) | Поле з дробовим числом (uk-UA / en-US) |
|---|---|---|---|---|
| Підписки | `/subscriptions` | `SubscriptionsPage` (лічильник у заголовку, список, empty-state) і `SubscriptionRow` (`@key="item.Id"`) після `SubscriptionState.Add` / `Remove` | «Мої підписки» / «My subscriptions» | `3,5` → `3.5m`, на екрані `≤ 3,5` / `3.5` → `3.5m`, на екрані `≤ 3.5` |
| Категорії | `/categories` | — (дані приходять параметром `Categories`, стану в сервісі немає) | «Категорії» / «Categories» | — |
| Календар цін | `/`, `/calendar` | — (дані приходять параметром `Prices`, стану в сервісі немає) | «Колоті дрова, дуб» / «Split firewood, oak»; місяці «Січ» / «Jan» | — |

## Стан і компоненти

- `SubscriptionState` — scoped-сервіс (`Services/SubscriptionState.cs`), подія `Changed`.
- `SubscriptionRow` — дочірній компонент: `[Parameter] Item`, `[Parameter] EventCallback<int> OnRemove`.
- `SubscribeFormComponent` — `[Parameter] EventCallback<SubscribeFormModel> OnSubmitted`.
- Списки з `@key`: рядки підписок, картки категорій, стовпці місяців.
- Поля форми: `@bind-Value` + `For` на `MudTextField` / `MudNumericField`.

## Тести, що підтверджують таблицю

| Що перевіряємо | Тест |
|---|---|
| 4 стани сторінки підписок | `SubscriptionsPageTests` (`Loading_…`, `Error_…`, `Empty_…`, `Data_…`) |
| Рендеринг після зміни стану | `SubscriptionsPageTests.AddingToState_ReRendersList`, `RemoveButton_RemovesRow_AndShowsEmptyState`, `SubmittingForm_AddsRowToList` |
| 4 стани категорій / календаря | `CategoriesPageTests`, `SeasonalCalendarPageTests` |
| Порожня форма → повідомлення під кожним обов'язковим полем | `SubscribeFormTests.EmptySubmit_ShowsMessageUnderEachRequiredField_…` |
| «3,5» для uk-UA і «3.5» для en-US | `FractionalFieldTests.NumericField_AcceptsFraction_ForCulture` |
| Значення поля на екрані за культурою | `FractionalFieldTests.SubscriptionsPage_ShowsThreshold_InCultureFormat` |
| Екран двома мовами | `LocalizationTests.SubscriptionsPage_UsesSelectedCulture`, `ScreenLanguageTests` |
| Вибір культури зберігається між візитами | `LocalizationTests.CultureService_PersistsChoice_AndReloads`, `LanguageButton_SavesChoice_ThroughCultureService` |
| Немає захардкоджених рядків | `LocalizationTests.RazorFiles_ContainNoHardcodedUkrainianText` |
