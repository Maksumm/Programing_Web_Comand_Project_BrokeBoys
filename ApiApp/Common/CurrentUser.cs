using System.Security.Claims;

namespace ApiApp.Common;

/// <summary>Хто саме викликає API: стабільний ідентифікатор користувача з токена Entra ID.</summary>
internal static class CurrentUser
{
    private const string LongObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>
    /// Ідентифікатор власника підписок: <c>oid</c> (id користувача в Entra ID), інакше <c>sub</c>.
    /// Не залежить від пошти, яку користувач може змінити; <c>null</c>, якщо токен не містить жодного з них.
    /// </summary>
    public static string? OwnerId(this ClaimsPrincipal user)
    {
        foreach (var value in new[]
        {
            user.FindFirst("oid")?.Value,
            user.FindFirst(LongObjectIdClaim)?.Value,
            user.FindFirst("sub")?.Value
        })
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
