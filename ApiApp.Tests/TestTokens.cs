using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ApiApp.Tests;

/// <summary>Підписує тестові JWT власним ключем; API перевіряє їх так само, як токени Entra ID (audience, строк дії, scope, ролі).</summary>
public static class TestTokens
{
    public const string TenantId = "11111111-1111-1111-1111-111111111111";
    public const string ApiClientId = "22222222-2222-2222-2222-222222222222";
    public const string Issuer = "https://login.test.example/issuer/v2.0";
    public const string Scope = "access_as_user";
    public const string AdminRole = "Admin";
    public const string SubscriberRole = "Subscriber";

    /// <summary>Ідентифікатор користувача (claim <c>oid</c>) у токенах за замовчуванням: власник демо-підписок у тестах.</summary>
    public const string OwnerId = "33333333-3333-3333-3333-333333333333";

    /// <summary>Audience API: Application ID URI.</summary>
    public const string ApiAudience = "api://" + ApiClientId;

    public static SymmetricSecurityKey SigningKey { get; } =
        new(Encoding.UTF8.GetBytes("test-signing-key-which-is-long-enough-for-hs256!"));

    public static string Create(
        string[]? roles = null,
        string? scope = Scope,
        string audience = ApiAudience,
        DateTime? notBefore = null,
        DateTime? expires = null,
        SecurityKey? key = null,
        string name = "Test User",
        string email = "tester@example.com",
        string? ownerId = OwnerId)
    {
        var claims = new Dictionary<string, object>
        {
            ["name"] = name,
            ["preferred_username"] = email
        };
        if (ownerId is not null)
        {
            claims["oid"] = ownerId;
        }

        if (scope is not null)
        {
            claims["scp"] = scope;
        }

        if (roles is { Length: > 0 })
        {
            claims["roles"] = roles;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            NotBefore = notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            Claims = claims,
            SigningCredentials = new SigningCredentials(key ?? SigningKey, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
