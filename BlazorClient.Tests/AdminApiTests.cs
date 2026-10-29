using System.Net;
using System.Net.Http.Json;
using BlazorClient.Models;
using BlazorClient.Services;
using Xunit;

namespace BlazorClient.Tests
{
    public class AdminApiTests
    {
        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            public List<(HttpMethod Method, string Path, string? Body)> Requests { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request.Method, request.RequestUri!.PathAndQuery, body));
                return respond(request);
            }
        }

        private static (AdminApi Api, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            var handler = new StubHandler(respond);
            return (new AdminApi(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") }), handler);
        }

        private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = JsonContent.Create(body) };

        [Fact]
        public async Task GetCategories_KeepsBothLanguages_OrderedById()
        {
            var (api, handler) = Create(_ => Json(new[]
            {
                new { id = 2, nameUk = "Шини", nameEn = "Tires", descriptionUk = "О2", descriptionEn = "D2", productsCount = 1, subscriptionsCount = 0 },
                new { id = 1, nameUk = "Дрова", nameEn = "Firewood", descriptionUk = "О1", descriptionEn = "D1", productsCount = 2, subscriptionsCount = 7 }
            }));

            var items = await api.GetCategoriesAsync();

            Assert.Equal(new[] { 1, 2 }, items.Select(c => c.Id));
            Assert.Equal(new AdminCategory(1, "Дрова", "Firewood", "О1", "D1", 2, 7), items[0]);
            Assert.Equal("/api/categories", handler.Requests.Single().Path);
        }

        [Fact]
        public async Task GetPrices_AsksForTheProduct_AndOrdersByMonth()
        {
            var (api, handler) = Create(_ => Json(new[]
            {
                new { id = 2, productId = 4, month = 10, price = 1500m },
                new { id = 1, productId = 4, month = 1, price = 1700m }
            }));

            var prices = await api.GetPricesAsync(4);

            Assert.Equal(new[] { 1, 10 }, prices.Select(p => p.Month));
            Assert.Equal("/api/prices?productId=4", handler.Requests.Single().Path);
        }

        [Fact]
        public async Task GetProducts_MapsTheFields()
        {
            var (api, _) = Create(_ => Json(new[] { new { id = 5, categoryId = 1, nameUk = "Дуб", nameEn = "Oak" } }));

            Assert.Equal(new AdminProduct(5, 1, "Дуб", "Oak"), Assert.Single(await api.GetProductsAsync()));
        }

        [Fact]
        public async Task CreateCategory_PostsAllFields_AndSucceedsOn201()
        {
            var (api, handler) = Create(_ => new HttpResponseMessage(HttpStatusCode.Created));

            var result = await api.CreateCategoryAsync(new CategoryInput("Цегла", "Bricks", "Опис", "Description"));

            Assert.True(result.Succeeded);
            var request = handler.Requests.Single();
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/categories", request.Path);

            // PostAsJsonAsync escapes non-ASCII letters (\u0426…), so compare the parsed values, not the raw text
            using var json = System.Text.Json.JsonDocument.Parse(request.Body!);
            Assert.Equal("Цегла", json.RootElement.GetProperty("nameUk").GetString());
            Assert.Equal("Bricks", json.RootElement.GetProperty("nameEn").GetString());
            Assert.Equal("Опис", json.RootElement.GetProperty("descriptionUk").GetString());
            Assert.Equal("Description", json.RootElement.GetProperty("descriptionEn").GetString());
        }

        [Fact]
        public async Task CreatePrice_PostsProductMonthAndPrice()
        {
            var (api, handler) = Create(_ => new HttpResponseMessage(HttpStatusCode.Created));

            var result = await api.CreatePriceAsync(new PriceInput(1, 3, 1500m));

            Assert.True(result.Succeeded);
            Assert.Contains("\"productId\":1", handler.Requests.Single().Body);
            Assert.Contains("\"month\":3", handler.Requests.Single().Body);
            Assert.Contains("\"price\":1500", handler.Requests.Single().Body);
        }

        [Fact]
        public async Task Replace_UsesPutOnTheRecordAddress_AndSucceedsOn204()
        {
            var (api, handler) = Create(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

            Assert.True((await api.ReplaceCategoryAsync(3, new CategoryInput("а", "b", "в", "d"))).Succeeded);
            Assert.True((await api.ReplaceProductAsync(4, new ProductInput(1, "а", "b"))).Succeeded);
            Assert.True((await api.ReplacePriceAsync(5, new PriceInput(1, 2, 9m))).Succeeded);

            Assert.All(handler.Requests, r => Assert.Equal(HttpMethod.Put, r.Method));
            Assert.Equal(new[] { "/api/categories/3", "/api/products/4", "/api/prices/5" }, handler.Requests.Select(r => r.Path));
        }

        [Fact]
        public async Task Delete_UsesDeleteOnTheRecordAddress()
        {
            var (api, handler) = Create(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

            Assert.True((await api.DeleteCategoryAsync(3)).Succeeded);
            Assert.True((await api.DeleteProductAsync(4)).Succeeded);
            Assert.True((await api.DeletePriceAsync(5)).Succeeded);

            Assert.All(handler.Requests, r => Assert.Equal(HttpMethod.Delete, r.Method));
            Assert.Equal(new[] { "/api/categories/3", "/api/products/4", "/api/prices/5" }, handler.Requests.Select(r => r.Path));
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, "ErrorInvalidData")]
        [InlineData(HttpStatusCode.Unauthorized, "ErrorSignInRequired")]
        [InlineData(HttpStatusCode.Forbidden, "ErrorForbidden")]
        [InlineData(HttpStatusCode.NotFound, "ErrorNotFound")]
        [InlineData(HttpStatusCode.Conflict, "ErrorConflict")]
        [InlineData(HttpStatusCode.InternalServerError, "ErrorApiUnavailable")]
        public async Task Writes_MapServerAnswersToMessageKeys(HttpStatusCode status, string key)
        {
            var (api, _) = Create(_ => new HttpResponseMessage(status));

            var result = await api.DeleteCategoryAsync(1);

            Assert.False(result.Succeeded);
            Assert.Equal(key, result.ErrorKey);
        }

        [Fact]
        public async Task Writes_WhenTheServerIsDown_ReturnUnavailableInsteadOfThrowing()
        {
            var (api, _) = Create(_ => throw new HttpRequestException("down"));

            var result = await api.CreatePriceAsync(new PriceInput(1, 1, 1m));

            Assert.False(result.Succeeded);
            Assert.Equal("ErrorApiUnavailable", result.ErrorKey);
        }
    }
}
