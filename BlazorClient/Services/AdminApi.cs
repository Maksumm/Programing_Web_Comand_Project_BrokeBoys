using System.Net;
using System.Net.Http.Json;
using BlazorClient.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BlazorClient.Services
{
    /// <summary>HTTP-клієнт до ApiApp для екранів адміністратора.</summary>
    public sealed class AdminApi(HttpClient http) : IAdminApi
    {
        private async Task<T> GetAsync<T>(string url) =>
            await http.GetFromJsonAsync<T>(url) ?? throw new InvalidOperationException($"Empty response from {url}.");

        public async Task<IReadOnlyList<AdminCategory>> GetCategoriesAsync() =>
            (await GetAsync<CategoryDto[]>("api/categories"))
                .OrderBy(c => c.Id)
                .Select(c => new AdminCategory(c.Id, c.NameUk, c.NameEn, c.DescriptionUk, c.DescriptionEn, c.ProductsCount, c.SubscriptionsCount))
                .ToList();

        public async Task<IReadOnlyList<AdminProduct>> GetProductsAsync() =>
            (await GetAsync<ProductDto[]>("api/products"))
                .OrderBy(p => p.Id)
                .Select(p => new AdminProduct(p.Id, p.CategoryId, p.NameUk, p.NameEn))
                .ToList();

        public async Task<IReadOnlyList<AdminPrice>> GetPricesAsync(int productId) =>
            (await GetAsync<PriceDto[]>($"api/prices?productId={productId}"))
                .OrderBy(p => p.Month)
                .Select(p => new AdminPrice(p.Id, p.ProductId, p.Month, p.Price))
                .ToList();

        public Task<ApiResult> CreateCategoryAsync(CategoryInput input) =>
            WriteAsync(() => http.PostAsJsonAsync("api/categories", input), HttpStatusCode.Created);

        public Task<ApiResult> ReplaceCategoryAsync(int id, CategoryInput input) =>
            WriteAsync(() => http.PutAsJsonAsync($"api/categories/{id}", input), HttpStatusCode.NoContent);

        public Task<ApiResult> DeleteCategoryAsync(int id) =>
            WriteAsync(() => http.DeleteAsync($"api/categories/{id}"), HttpStatusCode.NoContent);

        public Task<ApiResult> CreateProductAsync(ProductInput input) =>
            WriteAsync(() => http.PostAsJsonAsync("api/products", input), HttpStatusCode.Created);

        public Task<ApiResult> ReplaceProductAsync(int id, ProductInput input) =>
            WriteAsync(() => http.PutAsJsonAsync($"api/products/{id}", input), HttpStatusCode.NoContent);

        public Task<ApiResult> DeleteProductAsync(int id) =>
            WriteAsync(() => http.DeleteAsync($"api/products/{id}"), HttpStatusCode.NoContent);

        public Task<ApiResult> CreatePriceAsync(PriceInput input) =>
            WriteAsync(() => http.PostAsJsonAsync("api/prices", input), HttpStatusCode.Created);

        public Task<ApiResult> ReplacePriceAsync(int id, PriceInput input) =>
            WriteAsync(() => http.PutAsJsonAsync($"api/prices/{id}", input), HttpStatusCode.NoContent);

        public Task<ApiResult> DeletePriceAsync(int id) =>
            WriteAsync(() => http.DeleteAsync($"api/prices/{id}"), HttpStatusCode.NoContent);

        private static async Task<ApiResult> WriteAsync(Func<Task<HttpResponseMessage>> send, HttpStatusCode expected)
        {
            try
            {
                using var response = await send();
                return response.StatusCode == expected ? ApiResult.Ok() : ApiResult.Fail(ErrorKey(response.StatusCode));
            }
            catch (AccessTokenNotAvailableException)
            {
                return ApiResult.Fail("ErrorSignInRequired");
            }
            catch (HttpRequestException)
            {
                return ApiResult.Fail("ErrorApiUnavailable");
            }
        }

        private static string ErrorKey(HttpStatusCode status) => status switch
        {
            HttpStatusCode.BadRequest => "ErrorInvalidData",
            HttpStatusCode.Unauthorized => "ErrorSignInRequired",
            HttpStatusCode.Forbidden => "ErrorForbidden",
            HttpStatusCode.NotFound => "ErrorNotFound",
            HttpStatusCode.Conflict => "ErrorConflict",
            _ => "ErrorApiUnavailable"
        };
    }
}
