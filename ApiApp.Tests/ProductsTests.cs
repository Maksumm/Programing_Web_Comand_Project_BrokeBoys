using System.Net;
using System.Net.Http.Json;

namespace ApiApp.Tests;

public class ProductsTests : ApiTestBase
{
    private sealed record ProductDto(int Id, int CategoryId, string NameUk, string NameEn);

    private static object Valid(int categoryId = 1, string nameUk = "Брикети", string nameEn = "Briquettes") =>
        new { categoryId, nameUk, nameEn };

    [Fact]
    public async Task List_Returns200_AndFiltersByCategory()
    {
        var all = await (await Client.GetAsync("/api/products")).ReadAsync<List<ProductDto>>();
        var filtered = await (await Client.GetAsync("/api/products?categoryId=2")).ReadAsync<List<ProductDto>>();

        Assert.Equal(6, all.Count);
        Assert.NotEmpty(filtered);
        Assert.All(filtered, p => Assert.Equal(2, p.CategoryId));
    }

    [Fact]
    public async Task Get_Returns200_AndMissingReturns404()
    {
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/products/1")).StatusCode);
        await (await Client.GetAsync("/api/products/999")).AssertProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns201_WithLocation_ThatCanBeFetched()
    {
        var created = await Client.PostAsJsonAsync("/api/products", Valid());

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync(created.Headers.Location)).StatusCode);
    }

    [Fact]
    public async Task Create_Returns400_WhenFieldsAreMissing()
    {
        var problem = await (await Client.PostAsJsonAsync("/api/products", new { }))
            .AssertProblemAsync(HttpStatusCode.BadRequest);

        Assert.NotNull(problem.Errors);
        Assert.Contains("categoryId", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_Returns400_WhenCategoryDoesNotExist()
    {
        var problem = await (await Client.PostAsJsonAsync("/api/products", Valid(categoryId: 999)))
            .AssertProblemAsync(HttpStatusCode.BadRequest);

        Assert.NotNull(problem.Errors);
        Assert.Contains("categoryId", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameExistsInSameCategory()
    {
        await (await Client.PostAsJsonAsync("/api/products", Valid(1, "Колоті дрова, дуб", "Another")))
            .AssertProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Replace_Returns204_AndRepeatedReplaceReturns204Again()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/products/1", Valid())).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/products/1", Valid())).StatusCode);
        Assert.Equal("Briquettes", (await (await Client.GetAsync("/api/products/1")).ReadAsync<ProductDto>()).NameEn);
    }

    [Fact]
    public async Task Replace_Returns404_400_And409()
    {
        await (await Client.PutAsJsonAsync("/api/products/999", Valid())).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await Client.PutAsJsonAsync("/api/products/1", new { })).AssertProblemAsync(HttpStatusCode.BadRequest);
        await (await Client.PutAsJsonAsync("/api/products/1", Valid(1, "Колоті дрова, береза", "X")))
            .AssertProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_Returns204_RemovesPrices_ThenRepeatedDeleteReturns404()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/products/1")).StatusCode);
        await (await Client.DeleteAsync("/api/products/1")).AssertProblemAsync(HttpStatusCode.NotFound);

        var prices = await (await Client.GetAsync("/api/prices?productId=1")).ReadAsync<List<object>>();
        Assert.Empty(prices);
    }
}
