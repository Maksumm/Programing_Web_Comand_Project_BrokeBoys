using System.Security.Claims;
using ApiApp.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiApp.Features.Me;

/// <summary>Хто викликає API: ім'я, пошта й ролі з токена. Клієнт за цим вирішує, що показувати.</summary>
public sealed record MeResponse(string? Name, string? Email, string[] Roles);

internal static class GetMe
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet("/api/me", Handle)
            .WithName("GetMe")
            .WithTags("Me")
            .RequireAuthorization(Policies.Read)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

    private static Ok<MeResponse> Handle(ClaimsPrincipal user) =>
        TypedResults.Ok(new MeResponse(
            user.FindFirst("name")?.Value,
            user.FindFirst("preferred_username")?.Value ?? user.FindFirst("email")?.Value ?? user.FindFirst("upn")?.Value,
            user.FindAll("roles").Select(claim => claim.Value).Distinct().ToArray()));
}
