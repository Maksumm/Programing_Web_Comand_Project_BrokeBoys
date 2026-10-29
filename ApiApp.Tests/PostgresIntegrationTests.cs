using System.Net;
using System.Net.Http.Json;
using ApiApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace ApiApp.Tests;

/// <summary>Перевірки, яким потрібна справжня PostgreSQL: збереження, міграції, унікальні індекси, каскади, check-обмеження.</summary>
[Collection("Api")]
public class PostgresIntegrationTests(PostgresFixture fixture) : ApiTestBase(fixture)
{
    private sealed record CategoryDto(int Id, string NameUk, string NameEn);

    private static PostgresException? SqlError(Exception exception) =>
        exception as PostgresException ?? exception.InnerException as PostgresException;

    [Fact]
    public async Task Post_Returns201_AndGetByLocationHeader_Returns200()
    {
        var created = await Client.PostAsJsonAsync("/api/categories", new { nameUk = "Інструмент", nameEn = "Tools" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);

        var fetched = await Client.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal("Tools", (await fetched.ReadAsync<CategoryDto>()).NameEn);
    }

    [Fact]
    public async Task CreatedRecord_IsActuallyStoredInPostgres()
    {
        await Client.PostAsJsonAsync("/api/categories", new { nameUk = "Інструмент", nameEn = "Tools" });

        await using var db = Postgres.CreateDbContext();
        var stored = await db.Categories.AsNoTracking().SingleAsync(c => c.NameEn == "Tools");

        Assert.Equal("Інструмент", stored.NameUk);
    }

    [Fact]
    public async Task Migrations_AreApplied_AndMatchTheModel()
    {
        await using var db = Postgres.CreateDbContext();

        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "Модель відрізняється від міграцій: виконайте dotnet ef migrations add.");
    }

    [Fact]
    public async Task DeletingCategory_CascadesToItsSubscriptions()
    {
        var category = await (await Client.PostAsJsonAsync("/api/categories", new { nameUk = "Інструмент", nameEn = "Tools" }))
            .ReadAsync<CategoryDto>();
        var subscription = await Client.PostAsJsonAsync("/api/subscriptions", new { email = "cascade@example.com", categoryId = category.Id });
        Assert.Equal(HttpStatusCode.Created, subscription.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"/api/categories/{category.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync(subscription.Headers.Location)).StatusCode);
        await using var db = Postgres.CreateDbContext();
        Assert.False(await db.Subscriptions.AnyAsync(s => s.Email == "cascade@example.com"));
    }

    [Fact]
    public async Task DeletingCategoryWithProducts_IsRejectedByTheDatabase_AndNothingIsLost()
    {
        Assert.Equal(HttpStatusCode.Conflict, (await Client.DeleteAsync("/api/categories/1")).StatusCode);

        await using var db = Postgres.CreateDbContext();
        Assert.Equal(2, await db.Products.CountAsync(p => p.CategoryId == 1));
    }

    [Fact]
    public async Task UniqueIndexes_RejectDuplicates_InTheDatabase()
    {
        await using var db = Postgres.CreateDbContext();

        db.Categories.Add(new Category { NameUk = "Дрова", NameEn = "Something else" });
        var category = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal("23505", SqlError(category)?.SqlState);

        await using var db2 = Postgres.CreateDbContext();
        db2.SeasonalPrices.Add(new SeasonalPrice { ProductId = 1, Month = 1, Price = 1m });
        var price = await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
        Assert.Equal("23505", SqlError(price)?.SqlState);

        await using var db3 = Postgres.CreateDbContext();
        db3.Subscriptions.Add(new Subscription { OwnerId = TestTokens.OwnerId, Email = "someone-else@example.com", CategoryId = 1 });
        var subscription = await Assert.ThrowsAsync<DbUpdateException>(() => db3.SaveChangesAsync());
        Assert.Equal("23505", SqlError(subscription)?.SqlState);
    }

    [Fact]
    public async Task CheckConstraints_RejectImpossibleValues()
    {
        await using var db = Postgres.CreateDbContext();
        db.SeasonalPrices.Add(new SeasonalPrice { ProductId = 1, Month = 13, Price = 10m });

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal("23514", SqlError(error)?.SqlState);
    }

    [Fact]
    public async Task ForeignKeys_RejectMissingParents()
    {
        await using var db = Postgres.CreateDbContext();
        db.Products.Add(new Product { CategoryId = 999, NameUk = "Х", NameEn = "X" });

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal("23503", SqlError(error)?.SqlState);
    }

    [Fact]
    public async Task EmailsAreStoredInLowerCase()
    {
        var created = await Client.PostAsJsonAsync("/api/subscriptions", new { email = "NEW.User@Example.com", categoryId = 3 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        await using var db = Postgres.CreateDbContext();
        Assert.Equal("new.user@example.com", await db.Subscriptions.Where(s => s.CategoryId == 3).Select(s => s.Email).SingleAsync());
    }

    /// <summary>
    /// Друга міграція на базі з даними: підписки, створені до власника, отримують різні «legacy-…», тож унікальний індекс
    /// (OwnerId, CategoryId) створюється, навіть якщо в одній категорії було кілька різних пошт.
    /// </summary>
    [Fact]
    public async Task SecondMigration_GivesPreExistingSubscriptionsDistinctLegacyOwners()
    {
        var database = $"legacy_{Guid.NewGuid():N}";
        await using (var admin = Postgres.CreateDbContext())
        {
            await admin.Database.ExecuteSqlRawAsync($"CREATE DATABASE \"{database}\"");
        }

        var connectionString = new NpgsqlConnectionStringBuilder(Postgres.ConnectionString) { Database = database, Pooling = false }.ConnectionString;
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);

        // the schema as it was before ownership existed
        await db.GetService<IMigrator>().MigrateAsync("20261024234118_InitialCreate");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Categories\" (\"NameUk\", \"NameEn\", \"DescriptionUk\", \"DescriptionEn\") VALUES ('К', 'C', 'О', 'D')");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Subscriptions\" (\"Email\", \"CategoryId\", \"IsActive\") SELECT 'a@example.com', \"Id\", true FROM \"Categories\"");
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Subscriptions\" (\"Email\", \"CategoryId\", \"IsActive\") SELECT 'b@example.com', \"Id\", true FROM \"Categories\"");

        await db.Database.MigrateAsync();

        var owners = await db.Subscriptions.OrderBy(s => s.Id).Select(s => s.OwnerId).ToListAsync();
        Assert.Equal(2, owners.Count);
        Assert.Equal(2, owners.Distinct().Count());
        Assert.All(owners, owner => Assert.StartsWith("legacy-", owner));
    }
}
