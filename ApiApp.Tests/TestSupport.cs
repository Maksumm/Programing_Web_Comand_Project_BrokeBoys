using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using ApiApp.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Testcontainers.PostgreSql;

namespace ApiApp.Tests;

/// <summary>Хост API проти вказаної бази PostgreSQL. Демо-дані вимкнено: тест сам готує дані через <see cref="PostgresFixture.ResetAsync"/>.</summary>
public class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    /// <summary>Для тестів, яким база не потрібна (OpenAPI): підключення ніколи не відкривається.</summary>
    public const string NoDatabase = "Host=localhost;Port=1;Database=unused;Username=unused;Password=unused";

    public string EnvironmentName { get; init; } = "Development";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        // AddInMemoryCollection додається після appsettings*.json, тож має пріоритет над ними.
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Catalogue"] = connectionString,
            ["Seed:Demo"] = "false",
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd:TenantId"] = TestTokens.TenantId,
            ["AzureAd:ClientId"] = TestTokens.ApiClientId
        }));

        // Підміняємо лише джерело ключів підпису (замість метаданих Entra ID). Перевірка audience, терміну дії,
        // а також політики scope/ролей лишаються справжніми.
        builder.ConfigureServices(services => services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var configuration = new OpenIdConnectConfiguration { Issuer = TestTokens.Issuer };
            configuration.SigningKeys.Add(TestTokens.SigningKey);
            options.Configuration = configuration;
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
        }));
    }

    /// <summary>Клієнт із bearer-токеном (за замовчуванням — з scope і ролями Admin та Subscriber).</summary>
    public HttpClient CreateClientWithToken(string? token = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token ?? TestTokens.Create(roles: [TestTokens.AdminRole, TestTokens.SubscriberRole]));
        return client;
    }
}

/// <summary>
/// Один контейнер PostgreSQL на весь прогін тестів (Testcontainers). Схема створюється справжніми міграціями
/// (<c>MigrateAsync</c>), а перед кожним тестом таблиці очищаються й наповнюються демо-даними з тими самими id.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task ResetAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            @"TRUNCATE TABLE ""Subscriptions"", ""SeasonalPrices"", ""Products"", ""Categories"" RESTART IDENTITY CASCADE");
        await DemoData.SeedAsync(db, TestTokens.OwnerId);
    }
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<PostgresFixture>;

public abstract class ApiTestBase(PostgresFixture fixture) : IAsyncLifetime
{
    protected PostgresFixture Postgres { get; } = fixture;
    protected ApiFactory Factory { get; } = new(fixture.ConnectionString);
    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Postgres.ResetAsync();
        Client = Factory.CreateClientWithToken(); // з усіма правами; тести доступу беруть окремі токени
    }

    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        return Task.CompletedTask;
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
