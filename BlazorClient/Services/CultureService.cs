using Microsoft.JSInterop;

namespace BlazorClient.Services
{
    public class CultureService
    {
        private readonly IJSRuntime _js;

        public CultureService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task ChangeCultureAsync(string cultureName)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "client_culture", cultureName);
            await _js.InvokeVoidAsync("sessionStorage.setItem", "lang_switch", "1");
            await _js.InvokeVoidAsync("location.reload");
        }
    }
}