using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using BlazorClient.Models;
using BlazorClient.Resources;
using BlazorClient.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor.Services;
using Xunit;

// Тести змінюють CultureInfo (глобальний стан), тому паралельний запуск вимкнено.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BlazorClient.Tests
{
    internal static class TestHelpers
    {
        // Стани екранів створюються разом із контекстом і лежать тут, а не лише в DI: bUnit забороняє реєструвати сервіси
        // після того, як хоч один сервіс уже отримано (наприклад, тест узяв FakeCatalogueApi до виклику AddState).
        private static readonly ConditionalWeakTable<BunitContext, Dictionary<Type, object>> States = new();

        public static BunitContext CreateContext()
        {
            var ctx = new BunitContext();
            var states = new Dictionary<Type, object>
            {
                [typeof(ListState<SeasonalPrice>)] = new ListState<SeasonalPrice>(),
                [typeof(ListState<Category>)] = new ListState<Category>(),
                [typeof(ListState<Subscription>)] = new ListState<Subscription>()
            };
            States.Add(ctx, states);
            foreach (var (type, state) in states)
            {
                ctx.Services.AddSingleton(type, state);
            }

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddMudServices();
            ctx.Services.AddLocalization();

            var api = new FakeCatalogueApi();
            ctx.Services.AddSingleton(api);
            ctx.Services.AddSingleton<ICatalogueApi>(api);
            ctx.Services.AddScoped<CurrentUserState>();

            var admin = new FakeAdminApi();
            ctx.Services.AddSingleton(admin);
            ctx.Services.AddSingleton<IAdminApi>(admin);

            var live = new FakeLiveUpdates();
            ctx.Services.AddSingleton(live);
            ctx.Services.AddSingleton<ILiveUpdates>(live);
            return ctx;
        }

        /// <summary>Реєструє стан екрана як singleton, щоб тест міг змінювати його до і після рендеру.</summary>
        public static ListState<T> AddState<T>(this BunitContext ctx)
        {
            var states = States.GetValue(ctx, _ => new Dictionary<Type, object>());
            if (states.TryGetValue(typeof(ListState<T>), out var existing))
            {
                return (ListState<T>)existing;
            }

            // a state type that CreateContext does not know: register it the old way (only possible before any service is resolved)
            var state = new ListState<T>();
            ctx.Services.AddSingleton(state);
            states[typeof(ListState<T>)] = state;
            return state;
        }

        public static IStringLocalizer<AppResources> Localizer(this BunitContext ctx) =>
            ctx.Services.GetRequiredService<IStringLocalizer<AppResources>>();

        /// <summary>Вибране значення списку: Blazor показує його або атрибутом value, або позначкою selected на option.</summary>
        public static string? SelectedValue(this AngleSharp.Dom.IElement select) =>
            select.GetAttribute("value") ?? select.QuerySelector("option[selected]")?.GetAttribute("value");

        /// <summary>Розмітка з декодованими HTML-сутностями (апострофи, лапки).</summary>
        public static string Text(string markup) => WebUtility.HtmlDecode(markup);
    }

    /// <summary>Тимчасово змінює культуру і повертає попередню після завершення тесту.</summary>
    internal sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;
        private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        private readonly CultureInfo? _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

        public CultureScope(string name)
        {
            var culture = new CultureInfo(name);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
            CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
        }
    }
}
