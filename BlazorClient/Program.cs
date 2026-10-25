using Microsoft.AspNetCore.Components.Authorization;
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

// Режим E2E (лише для наскрізного тесту): готовий токен замість входу Microsoft. У бойовій конфігурації ключа "E2E:Token" немає.
var e2eToken = builder.Configuration["E2E:Token"];
if (string.IsNullOrEmpty(e2eToken))
{
    // Вхід через Microsoft Entra ID (MSAL). Authority і ClientId беруться з розділу AzureAd у wwwroot/appsettings.json.
    builder.Services.AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
        options.ProviderOptions.LoginMode = "redirect";
    });

    // Токен додається лише до запитів на адресу API.
    builder.Services.AddScoped<ApiAuthorizationMessageHandler>();
    builder.Services.AddScoped<DelegatingHandler>(sp => sp.GetRequiredService<ApiAuthorizationMessageHandler>());
    builder.Services.AddScoped<IAccessTokenSource, MsalAccessTokenSource>();
}
else
{
    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<AuthenticationStateProvider, E2EAuthenticationStateProvider>();
    builder.Services.AddScoped<DelegatingHandler>(_ => new StaticBearerHandler(e2eToken));
    builder.Services.AddScoped<IAccessTokenSource>(_ => new StaticAccessTokenSource(e2eToken));
}

builder.Services.AddScoped<ICatalogueApi>(sp =>
{
    var handler = sp.GetRequiredService<DelegatingHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new CatalogueApi(
        new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) },
        sp.GetRequiredService<IStringLocalizer<AppResources>>());
});
builder.Services.AddScoped<CurrentUserState>();

// Оновлення «наживо» через SignalR: з'єднання відкривається, коли сторінка приєднується до групи категорії.
builder.Services.AddScoped<IHubClient>(sp => new SignalRHubClient(
    new Uri(new Uri(apiBaseUrl), "hubs/prices").ToString(),
    sp.GetRequiredService<IAccessTokenSource>()));
builder.Services.AddScoped<ILiveUpdates, LiveUpdates>();

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
