using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using BlazorClient.Models;
using BlazorClient.Resources;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Localization;

namespace BlazorClient.Services
{
    /// <summary>HTTP-клієнт до ApiApp. Тексти для екранів вибираються за поточною культурою; назви місяців беруться з .resx.</summary>
    public sealed class CatalogueApi : ICatalogueApi
    {
        private static readonly string[] MonthKeys =
            { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        private readonly HttpClient _http;
        private readonly IStringLocalizer<AppResources> _l;

        public CatalogueApi(HttpClient http, IStringLocalizer<AppResources> localizer)
        {
            _http = http;
            _l = localizer;
        }

        private static string Pick(string uk, string en) =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "uk" ? uk : en;

        private static bool Same(string a, string b) =>
            string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

        private async Task<T> GetAsync<T>(string url) =>
            await _http.GetFromJsonAsync<T>(url) ?? throw new InvalidOperationException($"Empty response from {url}.");

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            var dtos = await GetAsync<CategoryDto[]>("api/categories");
            return dtos.Select(c => new Category
            {
                Id = c.Id,
                Name = Pick(c.NameUk, c.NameEn),
                Description = Pick(c.DescriptionUk, c.DescriptionEn),
                ProductsCount = c.ProductsCount,
                SubscriptionsCount = c.SubscriptionsCount
            }).ToList();
        }

        public async Task<IEnumerable<SeasonalPrice>> GetCalendarAsync()
        {
            var products = await GetAsync<ProductDto[]>("api/products");
            var product = products.OrderBy(p => p.Id).FirstOrDefault();
            return product is null ? new List<SeasonalPrice>() : await LoadPricesAsync(product);
        }

        public async Task<IEnumerable<SeasonalPrice>> GetCalendarAsync(int productId) =>
            await LoadPricesAsync(await GetAsync<ProductDto>($"api/products/{productId}"));

        public async Task<IEnumerable<ProductOption>> GetProductsAsync()
        {
            var products = await GetAsync<ProductDto[]>("api/products");
            return products
                .OrderBy(p => p.Id)
                .Select(p => new ProductOption(p.Id, p.CategoryId, Pick(p.NameUk, p.NameEn)))
                .ToList();
        }

        private async Task<List<SeasonalPrice>> LoadPricesAsync(ProductDto product)
        {
            var name = Pick(product.NameUk, product.NameEn);
            var prices = await GetAsync<PriceDto[]>($"api/prices?productId={product.Id}");
            return prices
                .Where(p => p.Month is >= 1 and <= 12)
                .OrderBy(p => p.Month)
                .Select(p => new SeasonalPrice
                {
                    Id = p.Id,
                    ProductId = product.Id,
                    Name = name,
                    Price = p.Price,
                    MonthNumber = p.Month,
                    CategoryId = product.CategoryId,
                    Month = _l["Month_" + MonthKeys[p.Month - 1]].Value
                }).ToList();
        }

        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<UserInfo>("api/me");
            }
            catch (Exception ex) when (ex is HttpRequestException or AccessTokenNotAvailableException)
            {
                return null;
            }
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptionsAsync()
        {
            var categories = (await GetAsync<CategoryDto[]>("api/categories")).ToDictionary(c => c.Id);
            var subscriptions = await GetAsync<SubscriptionDto[]>("api/subscriptions");
            return subscriptions.Select(s => ToModel(s, categories)).ToList();
        }

        public Task<ApiResult<Subscription>> CreateSubscriptionAsync(SubscribeFormModel form) => GuardAsync(async () =>
        {
            var categories = await GetAsync<CategoryDto[]>("api/categories");
            var match = categories.FirstOrDefault(c => Same(c.NameUk, form.Category) || Same(c.NameEn, form.Category));
            return match is null
                ? ApiResult<Subscription>.Fail("ErrorCategoryNotFound")
                : await PostSubscriptionAsync(categories, match.Id, form.Email, form.Threshold);
        });

        public Task<ApiResult<Subscription>> SubscribeToCategoryAsync(int categoryId, string email, decimal? threshold = null) => GuardAsync(async () =>
        {
            var categories = await GetAsync<CategoryDto[]>("api/categories");
            return categories.Any(c => c.Id == categoryId)
                ? await PostSubscriptionAsync(categories, categoryId, email, threshold)
                : ApiResult<Subscription>.Fail("ErrorCategoryNotFound");
        });

        private async Task<ApiResult<Subscription>> PostSubscriptionAsync(CategoryDto[] categories, int categoryId, string email, decimal? threshold)
        {
            var response = await _http.PostAsJsonAsync("api/subscriptions", new
            {
                email = email.Trim(),
                categoryId,
                threshold,
                isActive = true
            });
            if (response.StatusCode != HttpStatusCode.Created)
            {
                return ApiResult<Subscription>.Fail(ErrorKey(response.StatusCode));
            }

            var dto = await response.Content.ReadFromJsonAsync<SubscriptionDto>()
                ?? throw new InvalidOperationException("Empty response after creating a subscription.");
            return ApiResult<Subscription>.Ok(ToModel(dto, categories.ToDictionary(c => c.Id)));
        }

        private static async Task<ApiResult<Subscription>> GuardAsync(Func<Task<ApiResult<Subscription>>> action)
        {
            try
            {
                return await action();
            }
            catch (AccessTokenNotAvailableException)
            {
                return ApiResult<Subscription>.Fail("ErrorSignInRequired");
            }
            catch (HttpRequestException)
            {
                return ApiResult<Subscription>.Fail("ErrorApiUnavailable");
            }
        }

        public async Task<ApiResult> SetSubscriptionActiveAsync(Subscription subscription, bool isActive)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/subscriptions/{subscription.Id}", new
                {
                    email = subscription.Email,
                    categoryId = subscription.CategoryId,
                    threshold = subscription.Threshold,
                    isActive
                });
                return response.StatusCode == HttpStatusCode.NoContent ? ApiResult.Ok() : ApiResult.Fail(ErrorKey(response.StatusCode));
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

        public async Task<ApiResult> DeleteSubscriptionAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/subscriptions/{id}");
                return response.StatusCode == HttpStatusCode.NoContent ? ApiResult.Ok() : ApiResult.Fail(ErrorKey(response.StatusCode));
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
            HttpStatusCode.Conflict => "ErrorSubscriptionExists",
            _ => "ErrorApiUnavailable"
        };

        private static Subscription ToModel(SubscriptionDto dto, IReadOnlyDictionary<int, CategoryDto> categories) => new()
        {
            Id = dto.Id,
            Email = dto.Email,
            CategoryId = dto.CategoryId,
            CategoryName = categories.TryGetValue(dto.CategoryId, out var category) ? Pick(category.NameUk, category.NameEn) : $"#{dto.CategoryId}",
            Threshold = dto.Threshold,
            IsActive = dto.IsActive
        };
    }
}
