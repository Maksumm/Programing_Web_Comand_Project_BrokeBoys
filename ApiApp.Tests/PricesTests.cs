using System.Net;
using System.Net.Http.Json;

namespace ApiApp.Tests;

public class PricesTests : ApiTestBase
{
    private sealed record PriceDto(int Id, int ProductId, int Month, decimal Price);

    private static object Valid(int productId = 1, int month = 10, decimal price = 1500m) =>
        new { productId, month, price };

    [Fact]
    public async Task List_Returns200_With12MonthsPerProduct_AndFiltersByProduct()
    {
        var prices = await (await Client.GetAsync("/api/prices?productId=1")).ReadAsync<List<PriceDto>>();

        Assert.Equal(12, prices.Count);
        Assert.Equal(1500m, prices.Single(p => p.Month == 10).Price);
    }

    [Fact]
    public async Task Get_Returns200_AndMissingReturns404()
    {
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/prices/1")).StatusCode);
        await (await Client.GetAsync("/api/prices/9999")).AssertProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns201_WithLocation_ThatCanBeFetched()
    {
        // у демо-даних усі 12 місяців заповнені, тому спершу звільняємо місяць
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/prices/1")).StatusCode);

        var created = await Client.PostAsJsonAsync("/api/prices", Valid(1, 1, 1800m));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var fetched = await Client.GetAsync(created.Headers.Location);
        Assert.Equal(1800m, (await fetched.ReadAsync<PriceDto>()).Price);
    }

    [Fact]
    public async Task Create_Returns400_ForInvalidMonthPriceOrMissingProduct()
    {
        var bad = await (await Client.PostAsJsonAsync("/api/prices", new { productId = 1, month = 13, price = -5 }))
            .AssertProblemAsync(HttpStatusCode.BadRequest);
        Assert.NotNull(bad.Errors);
        Assert.Contains("month", bad.Errors.Keys);
        Assert.Contains("price", bad.Errors.Keys);

        var missing = await (await Client.PostAsJsonAsync("/api/prices", Valid(productId: 999)))
            .AssertProblemAsync(HttpStatusCode.BadRequest);
        Assert.NotNull(missing.Errors);
        Assert.Contains("productId", missing.Errors.Keys);
    }

    [Fact]
    public async Task Create_Returns409_WhenProductAlreadyHasPriceForMonth()
    {
        await (await Client.PostAsJsonAsync("/api/prices", Valid(1, 10, 999m)))
            .AssertProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Replace_Returns204_AndRepeatedReplaceReturns204Again()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/prices/10", Valid(1, 10, 1234m))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/prices/10", Valid(1, 10, 1234m))).StatusCode);
        Assert.Equal(1234m, (await (await Client.GetAsync("/api/prices/10")).ReadAsync<PriceDto>()).Price);
    }

    [Fact]
    public async Task Replace_Returns404_400_And409()
    {
        await (await Client.PutAsJsonAsync("/api/prices/9999", Valid())).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await Client.PutAsJsonAsync("/api/prices/10", new { })).AssertProblemAsync(HttpStatusCode.BadRequest);
        // ціна №10 переноситься на місяць, який уже зайнятий іншою ціною того ж продукту
        await (await Client.PutAsJsonAsync("/api/prices/10", Valid(1, 9, 1m))).AssertProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_Returns204_ThenRepeatedDeleteReturns404()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/prices/1")).StatusCode);
        await (await Client.DeleteAsync("/api/prices/1")).AssertProblemAsync(HttpStatusCode.NotFound);
    }
}
