using System.Security.Claims;
using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Subscriptions;

internal static class GetSubscription
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:int}", Handle)
            .WithName("GetSubscription")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<SubscriptionResponse>, ProblemHttpResult>> Handle(int id, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        // Чужа підписка відповідає так само, як відсутня: її існування не розкривається.
        var owner = user.OwnerId()!;
        var item = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == owner, ct);
        if (item is null)
        {
            return Problems.NotFound($"Subscription {id} was not found.");
        }

        return TypedResults.Ok(SubscriptionResponse.From(item));
    }
}
