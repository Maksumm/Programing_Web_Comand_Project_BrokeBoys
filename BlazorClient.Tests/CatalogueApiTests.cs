using System.Net;
using System.Net.Http.Json;
using BlazorClient.Models;
using BlazorClient.Services;
using Xunit;

namespace BlazorClient.Tests
{
    public class CatalogueApiTests
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

        private static readonly object[] Categories =
        {
            new { id = 1, nameUk = "Дрова", nameEn = "Firewood", descriptionUk = "Опис", descriptionEn = "Description", productsCount = 2, subscriptionsCount = 7 }
        };

        private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = JsonContent.Create(body) };

        private static (CatalogueApi Api, StubHandler Handler, BunitContextLease Lease) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            var lease = new BunitContextLease();
            var handler = new StubHandler(respond);
            var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
            return (new CatalogueApi(http, lease.Context.Localizer()), handler, lease);
        }

        private sealed class BunitContextLease : IAsyncDisposable
        {
            public Bunit.BunitContext Context { get; } = TestHelpers.CreateContext();

            public ValueTask DisposeAsync() => Context.DisposeAsync();
        }

        [Theory]
        [InlineData("uk-UA", "Дрова", "Опис")]
        [InlineData("en-US", "Firewood", "Description")]
        public async Task GetCategories_UsesTextsOfSelectedCulture_AndCounts(string culture, string name, string description)
        {
            using var scope = new CultureScope(culture);
            var (api, handler, lease) = Create(_ => Json(Categories));
            await using var leaseScope = lease;

            var items = (await api.GetCategoriesAsync()).ToList();

            var category = Assert.Single(items);
            Assert.Equal(name, category.Name);
            Assert.Equal(description, category.Description);
            Assert.Equal(7, category.SubscriptionsCount);
            Assert.Equal("/api/categories", handler.Requests.Single().Path);
        }

        [Fact]
        public async Task GetCalendar_LoadsPricesOfFirstProduct_WithMonthNumbersAndNames()
        {
            using var scope = new CultureScope("en-US");
            var (api, handler, lease) = Create(request => request.RequestUri!.AbsolutePath switch
            {
                "/api/products" => Json(new[] { new { id = 5, categoryId = 1, nameUk = "Дуб", nameEn = "Oak" } }),
                "/api/prices" => Json(new[]
                {
                    new { id = 2, productId = 5, month = 10, price = 1500m },
                    new { id = 1, productId = 5, month = 1, price = 1700m }
                }),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            });
            await using var leaseScope = lease;

            var prices = (await api.GetCalendarAsync()).ToList();

            Assert.Equal(new[] { 1, 10 }, prices.Select(p => p.MonthNumber));
            Assert.All(prices, p => Assert.Equal("Oak", p.Name));
            Assert.Equal(1500m, prices[1].Price);
            Assert.Equal(lease.Context.Localizer()["Month_Oct"].Value, prices[1].Month);
            Assert.Contains(handler.Requests, r => r.Path == "/api/prices?productId=5");
        }

        [Fact]
        public async Task GetCalendar_ReturnsEmpty_WhenThereAreNoProducts()
        {
            var (api, _, lease) = Create(_ => Json(Array.Empty<object>()));
            await using var leaseScope = lease;

            Assert.Empty(await api.GetCalendarAsync());
        }

        [Fact]
        public async Task GetSubscriptions_ResolvesCategoryName()
        {
            using var scope = new CultureScope("en-US");
            var (api, _, lease) = Create(request => request.RequestUri!.AbsolutePath == "/api/categories"
                ? Json(Categories)
                : Json(new[] { new { id = 3, email = "user@example.com", categoryId = 1, threshold = 90m, isActive = true } }));
            await using var leaseScope = lease;

            var item = Assert.Single(await api.GetSubscriptionsAsync());

            Assert.Equal("Firewood", item.CategoryName);
            Assert.Equal(1, item.CategoryId);
            Assert.Equal(90m, item.Threshold);
        }

        [Fact]
        public async Task CreateSubscription_Returns201AsSuccess_AndSendsCategoryId()
        {
            using var scope = new CultureScope("en-US");
            var (api, handler, lease) = Create(request => request.Method == HttpMethod.Get
                ? Json(Categories)
                : Json(new { id = 9, email = "a@b.com", categoryId = 1, threshold = 3.5m, isActive = true }, HttpStatusCode.Created));
            await using var leaseScope = lease;

            var result = await api.CreateSubscriptionAsync(new SubscribeFormModel { Email = "a@b.com", Category = "firewood", Threshold = 3.5m });

            Assert.True(result.Succeeded);
            Assert.Equal(9, result.Value!.Id);
            Assert.Equal("Firewood", result.Value.CategoryName);
            var post = handler.Requests.Single(r => r.Method == HttpMethod.Post);
            Assert.Equal("/api/subscriptions", post.Path);
            Assert.Contains("\"categoryId\":1", post.Body);
        }

        [Fact]
        public async Task CreateSubscription_WithUnknownCategory_FailsWithoutPosting()
        {
            var (api, handler, lease) = Create(_ => Json(Categories));
            await using var leaseScope = lease;

            var result = await api.CreateSubscriptionAsync(new SubscribeFormModel { Email = "a@b.com", Category = "Овочі" });

            Assert.False(result.Succeeded);
            Assert.Equal("ErrorCategoryNotFound", result.ErrorKey);
            Assert.DoesNotContain(handler.Requests, r => r.Method == HttpMethod.Post);
        }

        [Theory]
        [InlineData(HttpStatusCode.Conflict, "ErrorSubscriptionExists")]
        [InlineData(HttpStatusCode.BadRequest, "ErrorInvalidData")]
        [InlineData(HttpStatusCode.InternalServerError, "ErrorApiUnavailable")]
        public async Task CreateSubscription_MapsServerErrorsToMessageKeys(HttpStatusCode status, string key)
        {
            var (api, _, lease) = Create(request => request.Method == HttpMethod.Get
                ? Json(Categories)
                : new HttpResponseMessage(status));
            await using var leaseScope = lease;

            var result = await api.CreateSubscriptionAsync(new SubscribeFormModel { Email = "a@b.com", Category = "Дрова" });

            Assert.False(result.Succeeded);
            Assert.Equal(key, result.ErrorKey);
        }

        [Fact]
        public async Task CreateSubscription_WhenServerIsDown_ReturnsUnavailableInsteadOfThrowing()
        {
            var (api, _, lease) = Create(_ => throw new HttpRequestException("offline"));
            await using var leaseScope = lease;

            var result = await api.CreateSubscriptionAsync(new SubscribeFormModel { Email = "a@b.com", Category = "Дрова" });

            Assert.False(result.Succeeded);
            Assert.Equal("ErrorApiUnavailable", result.ErrorKey);
        }

        [Fact]
        public async Task SetSubscriptionActive_SendsPut_AndSucceedsOn204()
        {
            var (api, handler, lease) = Create(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
            await using var leaseScope = lease;
            var subscription = new Subscription { Id = 4, Email = "a@b.com", CategoryId = 2, Threshold = 80m };

            var result = await api.SetSubscriptionActiveAsync(subscription, false);

            Assert.True(result.Succeeded);
            var put = handler.Requests.Single();
            Assert.Equal(HttpMethod.Put, put.Method);
            Assert.Equal("/api/subscriptions/4", put.Path);
            Assert.Contains("\"isActive\":false", put.Body);
            Assert.Contains("\"categoryId\":2", put.Body);
        }

        [Fact]
        public async Task DeleteSubscription_Succeeds_On204_AndReportsNotFoundOn404()
        {
            var status = HttpStatusCode.NoContent;
            var (api, handler, lease) = Create(_ => new HttpResponseMessage(status));
            await using var leaseScope = lease;

            Assert.True((await api.DeleteSubscriptionAsync(4)).Succeeded);

            status = HttpStatusCode.NotFound;
            var missing = await api.DeleteSubscriptionAsync(4);

            Assert.False(missing.Succeeded);
            Assert.Equal("ErrorNotFound", missing.ErrorKey);
            Assert.All(handler.Requests, r => Assert.Equal(HttpMethod.Delete, r.Method));
        }
    }
}
