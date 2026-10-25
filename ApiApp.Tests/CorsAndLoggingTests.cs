using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiApp.Tests;

[Collection("Api")]
public class CorsAndLoggingTests(PostgresFixture fixture)
{
    private const string ClientOrigin = "https://localhost:7223";

    private static async Task<HttpResponseMessage> SendWithOriginAsync(HttpClient client, HttpMethod method, string url, string origin, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Origin", origin);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Cors_AllowsOnlyClientOrigin_AndExposesLocationHeader()
    {
        await fixture.ResetAsync();
        using var factory = new ApiFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var body = new { nameUk = "Інструмент", nameEn = "Tools" };

        var allowed = await SendWithOriginAsync(client, HttpMethod.Post, "/api/categories", ClientOrigin, body);
        var foreign = await SendWithOriginAsync(client, HttpMethod.Get, "/api/categories", "https://evil.example");

        Assert.Equal(ClientOrigin, allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("Location", allowed.Headers.GetValues("Access-Control-Expose-Headers").Single());
        Assert.False(foreign.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task EveryRequest_IsLogged_WithMethodPathStatusAndDuration()
    {
        await fixture.ResetAsync();
        var logs = new ListLoggerProvider();
        using var factory = new ApiFactory(fixture.ConnectionString).WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(logs)));
        using var client = factory.CreateClient();

        await client.GetAsync("/api/categories");
        await client.GetAsync("/api/categories/999");

        Assert.True(await EventuallyAsync(() => logs.Messages.Any(m => m.StartsWith("GET /api/categories responded 200 in ") && m.EndsWith(" ms"))));
        Assert.True(await EventuallyAsync(() => logs.Messages.Any(m => m.StartsWith("GET /api/categories/999 responded 404 in "))));
    }

    /// <summary>Рядок логу пишеться вже після відповіді, тож чекаємо на нього до 2 секунд.</summary>
    private static async Task<bool> EventuallyAsync(Func<bool> condition)
    {
        for (var i = 0; i < 40; i++)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(50);
        }

        return condition();
    }

    private sealed class ListLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _messages = [];

        public IReadOnlyList<string> Messages
        {
            get { lock (_messages) { return _messages.ToList(); } }
        }

        public ILogger CreateLogger(string categoryName) => new ListLogger(categoryName, _messages);

        public void Dispose() { }

        private sealed class ListLogger(string category, List<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!category.EndsWith("RequestLoggingMiddleware"))
                {
                    return;
                }

                lock (sink) { sink.Add(formatter(state, exception)); }
            }
        }
    }
}
