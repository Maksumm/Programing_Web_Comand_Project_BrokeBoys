using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Subscriptions;

internal static class DeleteSubscription
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeleteSubscription")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(int id, AppDbContext db, CancellationToken ct)
    {
        var removed = await db.Subscriptions.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
        if (removed == 0)
        {
            return Problems.NotFound($"Subscription {id} was not found.");
        }

        return TypedResults.NoContent();
    }
}
