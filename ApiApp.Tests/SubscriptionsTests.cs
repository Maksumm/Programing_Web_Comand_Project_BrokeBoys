using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Tests;

[Collection("Api")]
    public class SubscriptionsTests(PostgresFixture fixture) : ApiTestBase(fixture)
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

    private const string Other = "44444444-4444-4444-4444-444444444444";

    /// <summary>Клієнт іншого користувача з правами підписника (інший <c>oid</c> у токені).</summary>
    private HttpClient OtherUser() =>
        Factory.CreateClientWithToken(TestTokens.Create(roles: [TestTokens.SubscriberRole], ownerId: Other, email: "other@example.com"));

    [Fact]
    public async Task List_ReturnsOnlyTheCallersSubscriptions()
    {
        using var other = OtherUser();

        Assert.Empty(await (await other.GetAsync("/api/subscriptions")).ReadAsync<List<SubscriptionDto>>());

        var created = await other.PostAsJsonAsync("/api/subscriptions", Valid("other@example.com", 3));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var otherList = await (await other.GetAsync("/api/subscriptions")).ReadAsync<List<SubscriptionDto>>();
        var mine = await (await Client.GetAsync("/api/subscriptions")).ReadAsync<List<SubscriptionDto>>();

        Assert.Equal("other@example.com", Assert.Single(otherList).Email);
        Assert.Equal(2, mine.Count);
        Assert.DoesNotContain(mine, s => s.Email == "other@example.com");
    }

    [Fact]
    public async Task AnotherUsersSubscription_LooksLikeAMissingOne_ForGetPutAndDelete()
    {
        using var other = OtherUser();

        await (await other.GetAsync("/api/subscriptions/1")).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await other.PutAsJsonAsync("/api/subscriptions/1", Valid("other@example.com", 3))).AssertProblemAsync(HttpStatusCode.NotFound);
        await (await other.DeleteAsync("/api/subscriptions/1")).AssertProblemAsync(HttpStatusCode.NotFound);

        // the owner still has it, unchanged
        var item = await (await Client.GetAsync("/api/subscriptions/1")).ReadAsync<SubscriptionDto>();
        Assert.Equal("user@example.com", item.Email);
        Assert.Equal(1, item.CategoryId);
    }

    [Fact]
    public async Task TwoUsers_CanSubscribeToTheSameCategory_EvenWithTheSameEmail()
    {
        using var other = OtherUser();

        // category 1 is already followed by the default user with user@example.com
        var created = await other.PostAsJsonAsync("/api/subscriptions", Valid("user@example.com", 1));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task Create_StoresTheCallersOwnerId_NotAnythingFromTheBody()
    {
        using var other = OtherUser();

        var created = await other.PostAsJsonAsync("/api/subscriptions", new { email = "a@example.com", categoryId = 3, ownerId = TestTokens.OwnerId });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        await using var db = Postgres.CreateDbContext();
        var owner = await db.Subscriptions.Where(s => s.Email == "a@example.com").Select(s => s.OwnerId).SingleAsync();
        Assert.Equal(Other, owner);
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
    public async Task Create_Returns409_WhenTheCallerIsAlreadySubscribedToTheCategory()
    {
        await (await Client.PostAsJsonAsync("/api/subscriptions", Valid("user@example.com", 1)))
            .AssertProblemAsync(HttpStatusCode.Conflict);

        // the contact e-mail does not matter: the rule is one subscription per user and category
        await (await Client.PostAsJsonAsync("/api/subscriptions", Valid("another-address@example.com", 1)))
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
        // підписка №2 (шини) переноситься в категорію, на яку цей користувач уже підписаний (дрова)
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
