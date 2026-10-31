using BlazorClient.Components;
using BlazorClient.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class LiveToastsTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        private static readonly string[] MonthKeys =
            { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        private static Subscription Sub(int id, int categoryId, bool active = true) =>
            new() { Id = id, Email = "a@example.com", CategoryId = categoryId, CategoryName = "Категорія", IsActive = active };

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            var until = DateTime.UtcNow + Timeout;
            while (!condition() && DateTime.UtcNow < until)
            {
                await Task.Delay(20);
            }

            Assert.True(condition(), "the condition was not met in time");
        }

        private static string Month(BunitContext ctx, int month) => ctx.Localizer()["Month_" + MonthKeys[month - 1]].Value;

        [Fact]
        public async Task JoinsTheGroupsOfTheActiveSubscriptionsOnly()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1), Sub(2, 2, active: false), Sub(3, 3) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();

            ctx.Render<LiveToasts>();

            Assert.Equal(new[] { 1, 3 }, live.JoinedCategories.OrderBy(id => id));
        }

        [Fact]
        public async Task PriceChange_InAFollowedCategory_NamesTheProductMonthAndPrice()
        {
            using var culture = new CultureScope("en-US");
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new PriceChanged("Updated", 1, 5, 7, 1250m, "Дрова, дуб", "Split firewood, oak"));

            var expected = ctx.Localizer()["ToastPriceUpdated", "Split firewood, oak", Month(ctx, 7), "1250"].Value;
            Assert.Equal(expected, Assert.Single(toasts.Messages));
            Assert.Contains("Split firewood, oak", expected);
        }

        [Fact]
        public async Task TheProductNameFollowsTheInterfaceLanguage()
        {
            using var culture = new CultureScope("uk-UA");
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new PriceChanged("Updated", 1, 5, 7, 1250m, "Дрова, дуб", "Split firewood, oak"));

            Assert.Contains("Дрова, дуб", Assert.Single(toasts.Messages));
        }

        [Theory]
        [InlineData("Created", "ToastPriceCreated")]
        [InlineData("Updated", "ToastPriceUpdated")]
        public async Task PriceCreatedAndUpdated_ShowThePrice(string kind, string key)
        {
            using var culture = new CultureScope("en-US");
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new PriceChanged(kind, 1, 5, 3, 99.5m, "Дуб", "Oak"));

            Assert.Equal(ctx.Localizer()[key, "Oak", Month(ctx, 3), "99.5"].Value, Assert.Single(toasts.Messages));
        }

        [Fact]
        public async Task PriceDeleted_SaysItWasRemoved_WithoutAPrice()
        {
            using var culture = new CultureScope("en-US");
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new PriceChanged("Deleted", 1, 5, 11, null, "Дуб", "Oak"));

            Assert.Equal(ctx.Localizer()["ToastPriceDeleted", "Oak", Month(ctx, 11)].Value, Assert.Single(toasts.Messages));
        }

        [Fact]
        public async Task AMissingProductName_FallsBackToTheId()
        {
            using var culture = new CultureScope("en-US");
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new PriceChanged("Updated", 1, 5, 7, 10m));

            Assert.Contains("#5", Assert.Single(toasts.Messages));
        }

        [Theory]
        [InlineData("Product", "Created", "ToastProductCreated")]
        [InlineData("Product", "Updated", "ToastProductUpdated")]
        [InlineData("Product", "Deleted", "ToastProductDeleted")]
        [InlineData("Category", "Updated", "ToastCategoryUpdated")]
        [InlineData("Category", "Deleted", "ToastCategoryDeleted")]
        public async Task CatalogueChanges_InAFollowedCategory_NameWhatChanged(string entity, string kind, string key)
        {
            using var culture = new CultureScope("en-US");
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new CatalogueChanged(entity, kind, 1, entity == "Product" ? 8 : null, "Брикети", "Briquettes"));

            Assert.Equal(ctx.Localizer()[key, "Briquettes"].Value, Assert.Single(toasts.Messages));
        }

        [Fact]
        public async Task AnUnknownEntity_ShowsNothing()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new CatalogueChanged("Something", "Updated", 1, null, "X", "Y"));

            Assert.Empty(toasts.Messages);
        }

        [Fact]
        public async Task ChangesInOtherCategories_AndInSwitchedOffSubscriptions_ShowNothing()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1), Sub(2, 2, active: false) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new PriceChanged("Updated", 3, 5, 7, 10m, "Дуб", "Oak"));
            live.Raise(new PriceChanged("Updated", 2, 5, 7, 10m, "Дуб", "Oak"));
            live.Raise(new CatalogueChanged("Product", "Created", 3, 8, "A", "B"));
            live.Raise(new CatalogueChanged("Category", "Updated", 2, null, "A", "B"));

            Assert.Empty(toasts.Messages);
        }

        [Fact]
        public async Task SomeoneWithoutTheSubscriberRole_FollowsNothing_AndSeesNoToasts()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1) });
            ctx.Services.GetRequiredService<FakeCatalogueApi>().CurrentUser = new UserInfo("Admin only", "a@example.com", new[] { "Admin" });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new PriceChanged("Updated", 1, 5, 7, 10m, "Дуб", "Oak"));

            Assert.Empty(live.JoinedCategories);
            Assert.Empty(toasts.Messages);
        }

        [Fact]
        public async Task ANewSubscription_IsFollowedRightAway()
        {
            using var culture = new CultureScope("en-US");
            await using var ctx = TestHelpers.CreateContext();
            var state = ctx.AddState<Subscription>();
            state.SetItems(new List<Subscription>());
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            state.Add(Sub(9, 4));

            await WaitUntilAsync(() => live.JoinedCategories.Contains(4));
            live.Raise(new PriceChanged("Updated", 4, 5, 7, 10m, "Дуб", "Oak"));
            Assert.Single(toasts.Messages);
        }

        [Fact]
        public async Task AfterTheComponentIsDisposed_NoMoreToastsAppear()
        {
            // no "await using": the context is disposed explicitly below, which also disposes the rendered component
            var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription> { Sub(1, 1) });
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var toasts = ctx.Services.GetRequiredService<FakeToaster>();
            ctx.Render<LiveToasts>();

            live.Raise(new PriceChanged("Updated", 1, 5, 7, 10m, "Дуб", "Oak"));
            Assert.Single(toasts.Messages);

            await ctx.DisposeAsync();
            live.Raise(new PriceChanged("Updated", 1, 5, 7, 10m, "Дуб", "Oak"));

            Assert.Single(toasts.Messages);
        }
    }
}
