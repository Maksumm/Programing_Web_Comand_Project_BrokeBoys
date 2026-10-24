using System.Net;
using System.Net.Http.Json;

namespace ApiApp.Tests;

public class SubscriptionsTests : ApiTestBase
{
    private sealed record SubscriptionDto(int Id, string Email, int CategoryId, decimal? Threshold, bool IsActive);

    private static object Valid(string email = "new@example.com", int categoryId = 3, decimal? threshold = 3.5m, bool isActive = true) =>
        new { email, categoryId, threshold, isActive };

    [Fact]
    public async Task List_Returns200_AndFiltersByEmail()
    {
        var all = await (await Client.GetAsync("/api/subscriptions")).ReadAsync<List<SubscriptionDto>>();
        var mine = await (await Client.GetAsync("/api/subscriptions?email=USER@example.com")).ReadAsync<List<SubscriptionDto>>();
        var none = await (await Client.GetAsync("/api/subscriptions?email=nobody@example.com")).ReadAsync<List<SubscriptionDto>>();

        Assert.Equal(2, all.Count);
        Assert.Equal(2, mine.Count);
        Assert.Empty(none);
    }

    [Fact]
    public async Task Get_Returns200_AndMissingReturns404()
    {
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/subscriptions/1")).StatusCode);
        await (await Client.GetAsync("/api/subscriptions/999")).AssertProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns201_WithLocation_ThatCanBeFetched()
    {
        var created = await Client.PostAsJsonAsync("/api/subscriptions", Valid());

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var fetched = await (await Client.GetAsync(created.Headers.Location)).ReadAsync<SubscriptionDto>();
        Assert.Equal(3.5m, fetched.Threshold);
        Assert.True(fetched.IsActive);
    }

    [Fact]
    public async Task Create_Returns400_ForSameRulesAsTheClientForm()
    {
        var problem = await (await Client.PostAsJsonAsync("/api/subscriptions", new { email = "not-an-email", threshold = 0 }))
            .AssertProblemAsync(HttpStatusCode.BadRequest);

        Assert.NotNull(problem.Errors);
        Assert.Contains("email", problem.Errors.Keys);
        Assert.Contains("categoryId", problem.Errors.Keys);
        Assert.Contains("threshold", problem.Errors.Keys);

        var tooBig = await Client.PostAsJsonAsync("/api/subscriptions", Valid(threshold: 1001m));
        await tooBig.AssertProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns400_WhenCategoryDoesNotExist()
    {
        await (await Client.PostAsJsonAsync("/api/subscriptions", Valid(categoryId: 999)))
            .AssertProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenEmailAlreadySubscribedToCategory()
    {
        await (await Client.PostAsJsonAsync("/api/subscriptions", Valid("user@example.com", 1)))
            .AssertProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Replace_Returns204_AndRepeatedReplaceReturns204Again()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/subscriptions/1", Valid("user@example.com", 3, 2.5m, false))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/subscriptions/1", Valid("user@example.com", 3, 2.5m, false))).StatusCode);

        var item = await (await Client.GetAsync("/api/subscriptions/1")).ReadAsync<SubscriptionDto>();
        Assert.Equal(3, item.CategoryId);
        Assert.False(item.IsActive);
    }

    [Fact]
    public async Task Replace_Returns404_400_And409()
    {
        await (await Client.PutAsJsonAsync("/api/subscriptions/999", Valid())).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await Client.PutAsJsonAsync("/api/subscriptions/1", new { })).AssertProblemAsync(HttpStatusCode.BadRequest);
        // підписка №2 (шини) переноситься в категорію, на яку той самий e-mail уже підписаний (дрова)
        await (await Client.PutAsJsonAsync("/api/subscriptions/2", Valid("user@example.com", 1)))
            .AssertProblemAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_Returns204_ThenRepeatedDeleteReturns404()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync("/api/subscriptions/1")).StatusCode);
        await (await Client.DeleteAsync("/api/subscriptions/1")).AssertProblemAsync(HttpStatusCode.NotFound);
    }
}
