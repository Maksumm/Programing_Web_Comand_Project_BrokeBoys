using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Categories;

internal static class DeleteCategory
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeleteCategory")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(int id, AppDbContext db, CancellationToken ct)
    {
        try
        {
            var removed = await db.Categories.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
            if (removed == 0)
            {
                return Problems.NotFound($"Category {id} was not found.");
            }
        }
        catch (Exception ex) when (PgErrors.IsReferenceViolation(ex))
        {
            return Problems.Conflict("The category still has products; delete or move them first.");
        }

        return TypedResults.NoContent();
    }
}
