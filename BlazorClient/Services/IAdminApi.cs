using BlazorClient.Models;

namespace BlazorClient.Services
{
    /// <summary>
    /// Керування каталогом (лише для ролі Admin; сервер перевіряє роль сам). Читання кидають виняток при збої,
    /// запис повертає <see cref="ApiResult"/> з ключем повідомлення про помилку.
    /// </summary>
    public interface IAdminApi
    {
        Task<IReadOnlyList<AdminCategory>> GetCategoriesAsync();

        Task<IReadOnlyList<AdminProduct>> GetProductsAsync();

        Task<IReadOnlyList<AdminPrice>> GetPricesAsync(int productId);

        Task<ApiResult> CreateCategoryAsync(CategoryInput input);

        Task<ApiResult> ReplaceCategoryAsync(int id, CategoryInput input);

        Task<ApiResult> DeleteCategoryAsync(int id);

        Task<ApiResult> CreateProductAsync(ProductInput input);

        Task<ApiResult> ReplaceProductAsync(int id, ProductInput input);

        Task<ApiResult> DeleteProductAsync(int id);

        Task<ApiResult> CreatePriceAsync(PriceInput input);

        Task<ApiResult> ReplacePriceAsync(int id, PriceInput input);

        Task<ApiResult> DeletePriceAsync(int id);
    }
}
