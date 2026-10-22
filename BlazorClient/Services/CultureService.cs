using System.Globalization;
using Microsoft.JSInterop;

namespace BlazorClient.Services
{
    public class CultureService
    {
        public const string StorageKey = "client_culture";
        public const string DefaultCulture = "uk-UA";
        public static readonly string[] SupportedCultures = { "uk-UA", "en-US" };

        private readonly IJSRuntime _js;

        public CultureService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>Поточна культура інтерфейсу (для підсвічування кнопки УКР/ENG).</summary>
        public string CurrentCulture => CultureInfo.CurrentUICulture.Name;

        /// <summary>Повертає збережену культуру або типову, якщо її немає чи вона непідтримувана.</summary>
        public async Task<string> GetSavedCultureAsync()
        {
            var saved = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            return saved is not null && SupportedCultures.Contains(saved) ? saved : DefaultCulture;
        }

        /// <summary>Викликається в Program.cs до RunAsync(): застосовує збережену культуру.</summary>
        public async Task InitializeAsync()
        {
            var culture = new CultureInfo(await GetSavedCultureAsync());
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        /// <summary>Зберігає вибір користувача та перезавантажує застосунок, щоб усі сторінки оновились.</summary>
        public async Task ChangeCultureAsync(string cultureName)
        {
            if (!SupportedCultures.Contains(cultureName))
            {
                throw new ArgumentException($"Непідтримувана культура: {cultureName}", nameof(cultureName));
            }

            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, cultureName);
            await _js.InvokeVoidAsync("sessionStorage.setItem", "lang_switch", "1");
            await _js.InvokeVoidAsync("location.reload");
        }
    }
}
