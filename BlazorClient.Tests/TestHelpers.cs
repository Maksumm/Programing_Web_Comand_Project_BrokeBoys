using System.Globalization;
using System.Net;
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
        public static BunitContext CreateContext()
        {
            var ctx = new BunitContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddMudServices();
            ctx.Services.AddLocalization();

            var api = new FakeCatalogueApi();
            ctx.Services.AddSingleton(api);
            ctx.Services.AddSingleton<ICatalogueApi>(api);
            ctx.Services.AddScoped<CurrentUserState>();
            return ctx;
        }

        /// <summary>Реєструє стан екрана як singleton, щоб тест міг змінювати його до і після рендеру.</summary>
        public static ListState<T> AddState<T>(this BunitContext ctx)
        {
            var state = new ListState<T>();
            ctx.Services.AddSingleton(state);
            return state;
        }

        public static IStringLocalizer<AppResources> Localizer(this BunitContext ctx) =>
            ctx.Services.GetRequiredService<IStringLocalizer<AppResources>>();

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
