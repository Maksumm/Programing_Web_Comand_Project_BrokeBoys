using System.Globalization;
using Microsoft.JSInterop;

namespace BlazorClient.Services
{
    public class CultureService
    {
        private readonly IJSRuntime _jsRuntime;

        public CultureService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetCultureAsync(string cultureName)
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "blazorCulture", cultureName);
        }

        public async Task<string> GetCultureAsync()
        {
            var cultureName = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "blazorCulture");
            return string.IsNullOrEmpty(cultureName) ? "uk-UA" : cultureName;
        }
    }
}