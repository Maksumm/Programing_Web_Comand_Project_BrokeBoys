using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorClient;
using BlazorClient.Services;
using MudBlazor.Services;
using System.Globalization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddLocalization();
builder.Services.AddScoped<CultureService>();

var host = builder.Build();

var js = host.Services.GetRequiredService<IJSRuntime>();
string? cultureString = null;
try
{
    cultureString = await js.InvokeAsync<string?>("localStorage.getItem", "client_culture");
}
catch { }

var culture = new CultureInfo(string.IsNullOrEmpty(cultureString) ? "uk-UA" : cultureString);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();