using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorClient;
using BlazorClient.Models;
using BlazorClient.Services;
using Microsoft.Extensions.DependencyInjection;
using BlazorClient.Resources;
using Microsoft.Extensions.Localization;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Адреса API береться з wwwroot/appsettings.json (у проді її записує workflow релізу), а не з адреси самого клієнта.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured in wwwroot/appsettings.json.");
var apiScope = builder.Configuration["ApiScope"]
    ?? throw new InvalidOperationException("ApiScope is not configured in wwwroot/appsettings.json.");

// Вхід через Microsoft Entra ID (MSAL). Authority і ClientId беруться з розділу AzureAd у wwwroot/appsettings.json.
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
    options.ProviderOptions.LoginMode = "redirect";
});

// Токен додається лише до запитів на адресу API.
builder.Services.AddScoped<ApiAuthorizationMessageHandler>();
builder.Services.AddScoped<ICatalogueApi>(sp =>
{
    var handler = sp.GetRequiredService<ApiAuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new CatalogueApi(
        new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) },
        sp.GetRequiredService<IStringLocalizer<AppResources>>());
});
builder.Services.AddScoped<CurrentUserState>();

builder.Services.AddMudServices();
builder.Services.AddLocalization();
builder.Services.AddScoped<CultureService>();

// Стан екранів (scoped): сторінки читають і підписуються на зміни.
builder.Services.AddScoped<ListState<Category>>();
builder.Services.AddScoped<ListState<SeasonalPrice>>();
builder.Services.AddScoped<ListState<Subscription>>();

var host = builder.Build();

// Застосовуємо культуру, яку користувач обрав раніше (localStorage).
await host.Services.GetRequiredService<CultureService>().InitializeAsync();

await host.RunAsync();
