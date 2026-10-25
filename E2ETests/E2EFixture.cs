using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using ApiApp.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E2ETests;

/// <summary>
/// Піднімає все, що потрібно для наскрізного тесту: PostgreSQL у контейнері, API на Kestrel і справжній клієнт Blazor
/// (публікується командою <c>dotnet publish</c> і віддається окремим статичним хостом).
/// </summary>
public sealed class E2EFixture : IAsyncLifetime
{
    private readonly PostgresFixture _postgres = new();
    private KestrelApiFactory? _api;
    private WebApplication? _clientHost;
    private string? _publishDirectory;

    public string ApiUrl { get; private set; } = string.Empty;

    public string ClientUrl { get; private set; } = string.Empty;

    /// <summary>HTTP-клієнт до справжнього API (не до TestServer) з токеном адміністратора.</summary>
    public HttpClient ApiClient { get; private set; } = null!;

    public Task ResetAsync() => _postgres.ResetAsync();

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();
        await _postgres.ResetAsync();

        // Адресу клієнта потрібно знати до старту API: CORS дозволяє лише її.
        ClientUrl = $"http://127.0.0.1:{FreeTcpPort()}/";
        Environment.SetEnvironmentVariable("Cors__ClientOrigin", ClientUrl.TrimEnd('/'));

        _api = new KestrelApiFactory(_postgres.ConnectionString);
        _api.CreateClient().Dispose(); // запускає і TestServer, і Kestrel
        ApiUrl = new Uri(_api.ServerAddress).GetLeftPart(UriPartial.Authority) + "/";

        var token = TestTokens.Create(roles: [TestTokens.AdminRole, TestTokens.SubscriberRole]);
        ApiClient = new HttpClient { BaseAddress = new Uri(ApiUrl) };
        ApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _publishDirectory = Path.Combine(Path.GetTempPath(), "brokeboys-e2e-" + Guid.NewGuid().ToString("N"));
        await PublishClientAsync(_publishDirectory);
        WriteClientSettings(_publishDirectory, token);
        _clientHost = await StartClientHostAsync(_publishDirectory);
    }

    public async Task DisposeAsync()
    {
        ApiClient?.Dispose();
        if (_clientHost is not null)
        {
            await _clientHost.StopAsync();
            await _clientHost.DisposeAsync();
        }

        _api?.Dispose();
        await _postgres.DisposeAsync();
        Environment.SetEnvironmentVariable("Cors__ClientOrigin", null);

        if (_publishDirectory is not null && Directory.Exists(_publishDirectory))
        {
            try
            {
                Directory.Delete(_publishDirectory, recursive: true);
            }
            catch (IOException)
            {
                // тимчасова тека; система прибере її сама
            }
        }
    }

    private static int FreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorApp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("BlazorApp.sln was not found above " + AppContext.BaseDirectory);
    }

    private static async Task PublishClientAsync(string output)
    {
        var project = Path.Combine(FindRepositoryRoot(), "BlazorClient", "BlazorClient.csproj");
        var startInfo = new ProcessStartInfo("dotnet", $"publish \"{project}\" -c Release -o \"{output}\" --nologo -v minimal")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start dotnet publish.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("dotnet publish of BlazorClient took longer than 8 minutes.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet publish of BlazorClient failed:{Environment.NewLine}{await stdout}{Environment.NewLine}{await stderr}");
        }
    }

    /// <summary>
    /// Налаштування клієнта для тесту: адреса API і режим E2E (вхід підставляється тестовим токеном, MSAL не потрібен).
    /// API все одно перевіряє цей токен так само, як справжній.
    /// </summary>
    private void WriteClientSettings(string publishDirectory, string token)
    {
        var settings = new Dictionary<string, object>
        {
            ["ApiBaseUrl"] = ApiUrl,
            ["ApiScope"] = "api://e2e/access_as_user",
            ["E2E"] = new { Token = token },
            ["AzureAd"] = new { Authority = "https://login.microsoftonline.com/e2e", ClientId = "e2e" }
        };
        File.WriteAllText(Path.Combine(publishDirectory, "wwwroot", "appsettings.json"), JsonSerializer.Serialize(settings));
    }

    private async Task<WebApplication> StartClientHostAsync(string publishDirectory)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = publishDirectory,
            WebRootPath = Path.Combine(publishDirectory, "wwwroot")
        });
        builder.WebHost.UseUrls(ClientUrl);
        builder.Logging.ClearProviders();

        var app = builder.Build();
        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });
        app.MapFallbackToFile("index.html");
        await app.StartAsync();
        return app;
    }
}

[CollectionDefinition("E2E")]
public sealed class E2ECollection : ICollectionFixture<E2EFixture>;
