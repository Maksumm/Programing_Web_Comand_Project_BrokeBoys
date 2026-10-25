using ApiApp.Tests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace E2ETests;

/// <summary>
/// Справжній API на Kestrel із вільним портом: браузер Playwright не може ходити до TestServer, який живе лише в пам'яті.
/// Решта налаштувань (база, ключі підпису тестових токенів) — ті самі, що в тестах API.
/// </summary>
public sealed class KestrelApiFactory(string connectionString) : ApiFactory(connectionString)
{
    private IHost? _kestrelHost;

    /// <summary>Адреса, на якій слухає Kestrel, наприклад http://127.0.0.1:54321.</summary>
    public string ServerAddress { get; private set; } = string.Empty;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Хост для TestServer потрібен базовому класу; створюємо його до того, як перемкнемо builder на Kestrel.
        var testHost = builder.Build();

        builder.ConfigureWebHost(web => web.UseKestrel().UseUrls("http://127.0.0.1:0"));
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var addresses = _kestrelHost.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        ServerAddress = addresses.Addresses.First();

        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrelHost?.Dispose();
        }

        base.Dispose(disposing);
    }
}
