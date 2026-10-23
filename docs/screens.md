# Таблиця екранів

Усі екрани — Blazor WebAssembly + MudBlazor. Тести — bUnit + xUnit (`BlazorClient.Tests`).
Стан усіх екранів зберігається в одному generic scoped-сервісі `ListState<T>`
(`Services/ListState.cs`): стани `Loading` / `Error` / `Empty` / `Success` і подія `Changed`.
Сторінки підписуються на `Changed` в `OnInitializedAsync` і викликають `StateHasChanged`.

| Екран | Маршрут | Компоненти, що перерендерюються після зміни стану | Той самий екран двома мовами (uk-UA / en-US) | Поле з дробовим числом (uk-UA / en-US) |
|---|---|---|---|---|
| Підписки | `/subscriptions` | `SubscriptionsPage` (лічильник активних, список, empty-state) і `SubscriptionRow` (`@key="subscription.Id"`) після `ListState<Subscription>.SetItems` / `Add` | «Мої підписки» / «My subscriptions» | `3,5` → `3.5m`, у рядку `≤ 3,5`; `3.5` → `3.5m`, у рядку `≤ 3.5` |
| Категорії | `/categories` | `CategoriesPage` і `CategoryCard` (`@key="category.Id"`) після `ListState<Category>.SetItems` / `SetError` / `SetLoading` | «Категорії» / «Categories» | — |
| Календар цін | `/`, `/calendar` | `SeasonalCalendarPage` (12 стовпчиків місяців, `@key="m.Month"`) після `ListState<SeasonalPrice>.SetItems` / `SetError` / `SetLoading` | «Колоті дрова, дуб» / «Split firewood, oak»; місяці «Січ» / «Jan» | — |

## Компоненти

- `SubscriptionRow` — `[Parameter] Subscription`, `[Parameter] EventCallback<Subscription> OnToggled`.
- `CategoryCard` — `[Parameter] IsSelected`, `[Parameter] EventCallback<Category> OnSelected`.
- `SubscribeFormComponent` — `EditForm` + `DataAnnotationsValidator`, `[Parameter] EventCallback<SubscribeFormModel> OnSubmitted`.
- Поля форми: `@bind-Value` + `For` на `MudTextField` / `MudNumericField<decimal?>` (`Culture = CultureInfo.CurrentCulture`).
- Локалізація: `IStringLocalizer<AppResources>`, ресурси `AppResources.resx` / `.uk-UA.resx` / `.en-US.resx`.
- Вибір культури: `CultureService` (зберігає в `localStorage`, застосовує в `Program.cs` до `RunAsync()`).

## Тести, що підтверджують таблицю

| Що перевіряємо | Тест |
|---|---|
| 4 стани сторінки підписок | `SubscriptionsPageTests`: `LoadingState_…`, `ErrorState_…`, `EmptyState_…`, `SuccessState_…` |
| 4 стани категорій | `CategoriesPageTests`: `LoadingState_…`, `ErrorState_…`, `EmptyState_…`, `SuccessState_…`, `RetryButton_ReloadsData` |
| 4 стани календаря | `SeasonalCalendarPageTests`: `LoadingState_…`, `ErrorState_…`, `EmptyState_…`, `SuccessState_ShowsTwelveMonthBars` |
| Рендеринг після зміни стану | `CategoriesPageTests.ChangingState_ReRendersPage`, `SeasonalCalendarPageTests.ChangingState_ReRendersPage`, `SubscriptionsPageTests.SubmittingForm_AddsRow_AndReRendersFromEmptyToSuccess`, `SubscriptionsPageTests.TogglingSwitch_UpdatesActiveCount` |
| Порожня форма → повідомлення під кожним обов'язковим полем | `SubscribeFormTests.Should_Show_ValidationErrors_When_SubmittingEmptyForm`, `SubscriptionsPageTests.SubmittingEmptyForm_DoesNotAddRow` |
| «3,5» для uk-UA і «3.5» для en-US | `SubscribeFormTests.Threshold_ParsesFractionalNumber_ForCulture` |
| Значення поля на екрані за культурою | `SubscriptionsPageTests.Threshold_IsShownInRow_WithCultureSpecificSeparator`, `LocalizationTests.Localizer_FormatsNumbersByCulture` |
| Екран двома мовами | `LocalizationTests.Localizer_ReturnsTextForSelectedCulture`, `LocalizationTests.ValidationMessages_FollowSelectedCulture` |
| Вибір культури зберігається між візитами | `CultureServiceTests.GetSavedCulture_*`, `InitializeAsync_AppliesSavedCulture`, `ChangeCulture_SavesChoiceAndReloadsPage` |
