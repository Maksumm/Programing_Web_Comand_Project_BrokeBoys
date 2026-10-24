using System.Net;
using System.Net.Http.Json;

namespace ApiApp.Tests;

public class CategoriesTests : ApiTestBase
{
    private sealed record CategoryDto(int Id, string NameUk, string NameEn, int ProductsCount, int SubscriptionsCount);

    private static object Valid(string nameUk = "Інструмент", string nameEn = "Tools") =>
        new { nameUk, nameEn, descriptionUk = "Опис", descriptionEn = "Description" };

    [Fact]
    public async Task List_Returns200_WithSeededCategoriesAndCounts()
    {
        var response = await Client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.ReadAsync<List<CategoryDto>>();
        Assert.Equal(3, items.Count);
        Assert.All(items, c => Assert.True(c.ProductsCount > 0));
        Assert.Contains(items, c => c.SubscriptionsCount > 0);
    }

    [Fact]
    public async Task Get_Returns200_ForExistingCategory()
    {
        var response = await Client.GetAsync("/api/categories/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Firewood", (await response.ReadAsync<CategoryDto>()).NameEn);
    }

    [Fact]
    public async Task Get_Returns404_AsProblemDetails_ForMissingCategory()
    {
        await (await Client.GetAsync("/api/categories/999")).AssertProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns201_WithLocation_ThatCanBeFetched()
    {
        var created = await Client.PostAsJsonAsync("/api/categories", Valid());

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);

        var fetched = await Client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal("Tools", (await fetched.ReadAsync<CategoryDto>()).NameEn);
    }

    [Fact]
    public async Task Create_Returns400_WithFieldErrors_WhenFieldsAreMissing()
    {
        var problem = await (await Client.PostAsJsonAsync("/api/categories", new { }))
            .AssertProblemAsync(HttpStatusCode.BadRequest);

        Assert.NotNull(problem.Errors);
        Assert.Contains("nameUk", problem.Errors.Keys);
        Assert.Contains("nameEn", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameAlreadyExists()
    {
        await (await Client.PostAsJsonAsync("/api/categories", Valid(nameUk: "Дрова")))
            .AssertProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Replace_Returns204_AndRepeatedReplaceReturns204Again()
    {
        var first = await Client.PutAsJsonAsync("/api/categories/1", Valid("Паливо", "Fuel"));
        var second = await Client.PutAsJsonAsync("/api/categories/1", Valid("Паливо", "Fuel"));

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal("Fuel", (await (await Client.GetAsync("/api/categories/1")).ReadAsync<CategoryDto>()).NameEn);
    }

    [Fact]
    public async Task Replace_Returns404_ForMissingCategory()
    {
        await (await Client.PutAsJsonAsync("/api/categories/999", Valid()))
            .AssertProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Replace_Returns400_WhenFieldsAreMissing()
    {
        await (await Client.PutAsJsonAsync("/api/categories/1", new { }))
            .AssertProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Replace_Returns409_WhenNameBelongsToAnotherCategory()
    {
        await (await Client.PutAsJsonAsync("/api/categories/1", Valid(nameUk: "Шини")))
            .AssertProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_Returns409_WhenCategoryHasProducts()
    {
        await (await Client.DeleteAsync("/api/categories/1")).AssertProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_Returns204_ThenRepeatedDeleteReturns404()
    {
        var created = await Client.PostAsJsonAsync("/api/categories", Valid());
        var location = created.Headers.Location!;

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync(location)).StatusCode);
        await (await Client.DeleteAsync(location)).AssertProblemAsync(HttpStatusCode.NotFound);
    }
}
