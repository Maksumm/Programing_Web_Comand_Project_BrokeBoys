using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Subscriptions;

internal static class ListSubscriptions
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName("ListSubscriptions");

    private static async Task<Ok<SubscriptionResponse[]>> Handle(string? email, AppDbContext db, CancellationToken ct)
    {
        var normalized = email?.Trim().ToLowerInvariant();
        var items = await db.Subscriptions.AsNoTracking()
            .Include(s => s.Category) // категорія завантажується тим самим запитом
            .Where(s => normalized == null || s.Email == normalized)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);
        return TypedResults.Ok(items.Select(s => SubscriptionResponse.From(s)).ToArray());
    }
}
