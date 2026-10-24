using System.Net;
using System.Net.Http.Json;
using ApiApp.Data;
using Microsoft.EntityFrameworkCore;
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
        db3.Subscriptions.Add(new Subscription { Email = "user@example.com", CategoryId = 1 });
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
    public async Task EmailsAreStoredInLowerCase_SoDuplicatesDifferingByCaseConflict()
    {
        var response = await Client.PostAsJsonAsync("/api/subscriptions", new { email = "USER@Example.com", categoryId = 1 });

        await response.AssertProblemAsync(HttpStatusCode.Conflict);
    }
}
