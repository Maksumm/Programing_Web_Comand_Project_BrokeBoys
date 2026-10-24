using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Subscriptions;

public sealed record CreateSubscriptionRequest(string? Email, int? CategoryId, decimal? Threshold, bool IsActive = true);

internal static class CreateSubscription
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreateSubscription")
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<SubscriptionResponse>, ValidationProblem, ProblemHttpResult>> Handle(CreateSubscriptionRequest request, AppDbContext db, CancellationToken ct)
    {
        var errors = new ErrorBag()
            .Email(nameof(request.Email), request.Email)
            .RequiredId(nameof(request.CategoryId), request.CategoryId)
            .Threshold(nameof(request.Threshold), request.Threshold);
        if (errors.HasErrors)
        {
            return errors.ToResult();
        }

        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
        {
            return Problems.Invalid(nameof(request.CategoryId), "Category does not exist.");
        }

        var entity = new Subscription
        {
            Email = request.Email!.Trim().ToLowerInvariant(),
            CategoryId = category.Id,
            Threshold = request.Threshold,
            IsActive = request.IsActive
        };
        db.Subscriptions.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.UniqueViolation))
        {
            return Problems.Conflict("This e-mail is already subscribed to this category.");
        }
        catch (DbUpdateException ex) when (PgErrors.Is(ex, PgErrors.ForeignKeyViolation))
        {
            return Problems.Invalid("CategoryId", "Category does not exist.");
        }

        return TypedResults.Created($"/api/subscriptions/{entity.Id}", SubscriptionResponse.From(entity, category));
    }
}
