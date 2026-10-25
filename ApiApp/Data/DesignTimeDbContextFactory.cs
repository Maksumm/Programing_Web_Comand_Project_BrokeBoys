using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApiApp.Data;

/// <summary>
/// Для команд <c>dotnet ef migrations add</c> і <c>dotnet ef database update</c>: бере рядок підключення з user-secrets
/// (<c>ConnectionStrings:Catalogue</c>) і не запускає весь застосунок. Для «migrations add» з'єднання не відкривається,
/// тож достатньо й запасного значення.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string Fallback = "Host=localhost;Port=5433;Database=catalogue;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(configuration.GetConnectionString("Catalogue") ?? Fallback)
            .Options;
        return new AppDbContext(options);
    }
}
