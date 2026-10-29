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
            if (product is null)
            {
                return new List<SeasonalPrice>();
            }

            var name = Pick(product.NameUk, product.NameEn);
            var prices = await GetAsync<PriceDto[]>($"api/prices?productId={product.Id}");
            return prices
                .Where(p => p.Month is >= 1 and <= 12)
                .OrderBy(p => p.Month)
                .Select(p => new SeasonalPrice
                {
                    Id = p.Id,
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

        public async Task<ApiResult<Subscription>> CreateSubscriptionAsync(SubscribeFormModel form)
        {
            try
            {
                var categories = await GetAsync<CategoryDto[]>("api/categories");
                var match = categories.FirstOrDefault(c => Same(c.NameUk, form.Category) || Same(c.NameEn, form.Category));
                if (match is null)
                {
                    return ApiResult<Subscription>.Fail("ErrorCategoryNotFound");
                }

                var response = await _http.PostAsJsonAsync("api/subscriptions", new
                {
                    email = form.Email.Trim(),
                    categoryId = match.Id,
                    threshold = form.Threshold,
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
