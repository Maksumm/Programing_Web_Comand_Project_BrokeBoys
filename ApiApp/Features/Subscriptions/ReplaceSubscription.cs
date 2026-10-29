using System.Security.Claims;
using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Subscriptions;

public sealed record ReplaceSubscriptionRequest(string? Email, int? CategoryId, decimal? Threshold, bool? IsActive);

internal static class ReplaceSubscription
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:int}", Handle)
            .WithName("ReplaceSubscription")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> Handle(int id, ReplaceSubscriptionRequest request, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var errors = new ErrorBag()
            .Email(nameof(request.Email), request.Email)
            .RequiredId(nameof(request.CategoryId), request.CategoryId)
            .Threshold(nameof(request.Threshold), request.Threshold)
            .RequiredBool(nameof(request.IsActive), request.IsActive);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var owner = user.OwnerId()!;
        var entity = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == owner, ct);
        if (entity is null)
        {
            return Problems.NotFound($"Subscription {id} was not found.");
        }

        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
        {
            return Problems.Invalid(nameof(request.CategoryId), "Category does not exist.");
        }

        entity.Email = request.Email!.Trim().ToLowerInvariant();
        entity.CategoryId = category.Id;
        entity.Threshold = request.Threshold;
        entity.IsActive = request.IsActive!.Value;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.UniqueViolation))
        {
            return Problems.Conflict("You are already subscribed to this category.");
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.ForeignKeyViolation))
        {
            return Problems.Invalid("CategoryId", "Category does not exist.");
        }

        return TypedResults.NoContent();
    }
}
