using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Products;

internal static class DeleteProduct
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:int}", Handle)
            .WithName("DeleteProduct")
            .RequireAuthorization(Policies.ManageCatalogue)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(int id, AppDbContext db, CancellationToken ct)
    {
        var removed = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync(ct); // ціни видаляє каскад бази
        if (removed == 0)
        {
            return Problems.NotFound($"Product {id} was not found.");
        }

        return TypedResults.NoContent();
    }
}
