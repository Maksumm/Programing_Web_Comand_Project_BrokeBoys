using System.Net;
using System.Net.Http.Json;
using Microsoft.IdentityModel.Tokens;

namespace ApiApp.Tests;

/// <summary>401 і 403 для кожного захищеного endpoint-а, а також успіх із потрібною роллю.</summary>
[Collection("Api")]
public class AuthorizationTests(PostgresFixture fixture) : ApiTestBase(fixture)
{
    private sealed record MeDto(string? Name, string? Email, string[] Roles);

    private static readonly string[] Admin = [TestTokens.AdminRole];
    private static readonly string[] Subscriber = [TestTokens.SubscriberRole];

    // (метод, адреса, тіло) для кожної операції API.
    public static IEnumerable<object[]> Operations()
    {
        foreach (var resource in new[] { "categories", "products", "prices", "subscriptions" })
        {
            yield return ["GET", $"/api/{resource}"];
            yield return ["GET", $"/api/{resource}/1"];
            yield return ["POST", $"/api/{resource}"];
            yield return ["PUT", $"/api/{resource}/1"];
            yield return ["DELETE", $"/api/{resource}/1"];
        }

        yield return ["GET", "/api/me"];
    }

    private static HttpRequestMessage Request(string method, string url) =>
        new(new HttpMethod(method), url) { Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null };

    private async Task<HttpResponseMessage> SendAsync(string method, string url, string? token)
    {
        using var client = Factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        }

        return await client.SendAsync(Request(method, url));
    }

    // ---------- 401 ----------

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task WithoutToken_Returns401_AsProblemDetails(string method, string url)
    {
        await (await SendAsync(method, url, token: null)).AssertProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task TokenForAnotherApi_Returns401(string method, string url)
    {
        var token = TestTokens.Create(roles: [TestTokens.AdminRole, TestTokens.SubscriberRole], audience: "api://99999999-9999-9999-9999-999999999999");

        await (await SendAsync(method, url, token)).AssertProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task ExpiredToken_Returns401(string method, string url)
    {
        var token = TestTokens.Create(
            roles: [TestTokens.AdminRole, TestTokens.SubscriberRole],
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1));

        await (await SendAsync(method, url, token)).AssertProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TokenSignedWithForeignKey_Returns401()
    {
        var foreign = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("another-key-which-is-also-long-enough-for-hs256!"));
        var token = TestTokens.Create(roles: Admin, key: foreign);

        await (await SendAsync("GET", "/api/categories", token)).AssertProblemAsync(HttpStatusCode.Unauthorized);
    }

    // ---------- 403 ----------

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task TokenWithoutScope_Returns403(string method, string url)
    {
        var token = TestTokens.Create(roles: [TestTokens.AdminRole, TestTokens.SubscriberRole], scope: null);

        await (await SendAsync(method, url, token)).AssertProblemAsync(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("POST", "/api/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/4")]
    [InlineData("POST", "/api/products")]
    [InlineData("PUT", "/api/products/1")]
    [InlineData("DELETE", "/api/products/1")]
    [InlineData("POST", "/api/prices")]
    [InlineData("PUT", "/api/prices/1")]
    [InlineData("DELETE", "/api/prices/1")]
    public async Task CatalogueChanges_WithoutAdminRole_Return403(string method, string url)
    {
        var withoutRole = TestTokens.Create(roles: null);
        var subscriberOnly = TestTokens.Create(roles: Subscriber);

        await (await SendAsync(method, url, withoutRole)).AssertProblemAsync(HttpStatusCode.Forbidden);
        await (await SendAsync(method, url, subscriberOnly)).AssertProblemAsync(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("GET", "/api/subscriptions")]
    [InlineData("GET", "/api/subscriptions/1")]
    [InlineData("POST", "/api/subscriptions")]
    [InlineData("PUT", "/api/subscriptions/1")]
    [InlineData("DELETE", "/api/subscriptions/1")]
    public async Task Subscriptions_WithoutSubscriberRole_Return403(string method, string url)
    {
        await (await SendAsync(method, url, TestTokens.Create(roles: null))).AssertProblemAsync(HttpStatusCode.Forbidden);
        await (await SendAsync(method, url, TestTokens.Create(roles: Admin))).AssertProblemAsync(HttpStatusCode.Forbidden);
    }

    // ---------- success with the needed role ----------

    [Theory]
    [InlineData("/api/categories")]
    [InlineData("/api/products")]
    [InlineData("/api/prices")]
    public async Task Reading_NeedsOnlyTheScope(string url)
    {
        var response = await SendAsync("GET", url, TestTokens.Create(roles: null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanChangeTheCatalogue()
    {
        using var client = Factory.CreateClientWithToken(TestTokens.Create(roles: Admin));

        var created = await client.PostAsJsonAsync("/api/categories", new { nameUk = "Інструмент", nameEn = "Tools" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(created.Headers.Location)).StatusCode);
    }

    [Fact]
    public async Task Subscriber_CanManageSubscriptions()
    {
        using var client = Factory.CreateClientWithToken(TestTokens.Create(roles: Subscriber));

        var created = await client.PostAsJsonAsync("/api/subscriptions", new { email = "new@example.com", categoryId = 3, threshold = 3.5m });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/subscriptions")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(created.Headers.Location)).StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsNameEmailAndRolesFromTheToken()
    {
        var token = TestTokens.Create(roles: [TestTokens.AdminRole, TestTokens.SubscriberRole], name: "Олена", email: "olena@example.com");
        var response = await SendAsync("GET", "/api/me", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.ReadAsync<MeDto>();
        Assert.Equal("Олена", me.Name);
        Assert.Equal("olena@example.com", me.Email);
        Assert.Equivalent(new[] { "Admin", "Subscriber" }, me.Roles);
    }

    [Fact]
    public async Task OpenApiDocument_StaysPublic()
    {
        using var client = Factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }
}
