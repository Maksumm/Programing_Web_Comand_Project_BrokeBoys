using System.Globalization;
using Bunit;
using BlazorClient.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace BlazorClient.Tests;

public static class TestHelpers
{
    /// <summary>
    /// Контекст bUnit з MudBlazor, локалізацією та сервісами застосунку.
    /// Культура встановлюється для поточного тесту (async-local), тож тести не заважають один одному.
    /// </summary>
    public static BunitContext CreateContext(string culture = "uk-UA")
    {
        var ci = new CultureInfo(culture);
        CultureInfo.CurrentCulture = ci;
        CultureInfo.CurrentUICulture = ci;

        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddLocalization();
        ctx.Services.AddScoped<SubscriptionState>();
        ctx.Services.AddScoped<CultureService>();
        return ctx;
    }
}

// Залишено для ще не переписаних тестів сторінок (буде прибрано в гілці fix/real-tests)
public class DummyHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent("[]")
        });
}
