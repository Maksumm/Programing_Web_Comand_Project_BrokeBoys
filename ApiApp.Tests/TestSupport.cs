using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ApiApp.Tests;

/// <summary>Окремий хост API; кожен тест створює свій, тож дані in-memory не перетинаються між тестами.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public string EnvironmentName { get; init; } = "Development";

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment(EnvironmentName);
}

public abstract class ApiTestBase : IDisposable
{
    protected ApiFactory Factory { get; } = new();
    protected HttpClient Client { get; }

    protected ApiTestBase() => Client = Factory.CreateClient();

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Тіло ProblemDetails / ValidationProblemDetails для перевірок.</summary>
public sealed record ProblemBody(int? Status, string? Title, string? Detail, Dictionary<string, string[]>? Errors);

public static class HttpAssertions
{
    /// <summary>Перевіряє код, тип application/problem+json і поле status у тілі.</summary>
    public static async Task<ProblemBody> AssertProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<ProblemBody>();
        Assert.NotNull(body);
        Assert.Equal((int)expected, body.Status);
        return body;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(value);
        return value;
    }
}
