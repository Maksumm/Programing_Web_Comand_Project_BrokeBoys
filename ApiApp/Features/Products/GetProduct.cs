using ApiApp.Common;
using ApiApp.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Features.Products;

internal static class GetProduct
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:int}", Handle)
            .WithName("GetProduct")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> Handle(int id, AppDbContext db, CancellationToken ct)
    {
        var item = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (item is null)
        {
            return Problems.NotFound($"Product {id} was not found.");
        }

        return TypedResults.Ok(ProductResponse.From(item));
    }
}
