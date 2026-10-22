using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorClient;
using BlazorClient.Models;
using BlazorClient.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

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
