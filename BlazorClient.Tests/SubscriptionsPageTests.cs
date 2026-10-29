using BlazorClient.Models;
using BlazorClient.Pages;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class SubscriptionsPageTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        private static void Seed(BunitContext ctx, params string[] names)
        {
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();
            foreach (var name in names)
            {
                api.Categories.Add(new Category { Id = api.Categories.Count + 1, Name = name });
            }
        }

        private static List<Subscription> Sample() => new()
        {
            new Subscription { Id = 1, Email = "a@example.com", CategoryName = "Дрова", Threshold = 90m },
            new Subscription { Id = 2, Email = "b@example.com", CategoryName = "Шини" }
        };

        [Fact]
        public async Task LoadingState_ShowsSpinnerAndLoadingText()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetLoading();

            var cut = ctx.Render<SubscriptionsPage>();

            Assert.Single(cut.FindAll(".mud-progress-circular"));
            Assert.Contains(ctx.Localizer()["LoadingSubscriptions"].Value, TestHelpers.Text(cut.Markup));
            Assert.Empty(cut.FindAll("form"));
        }

        [Fact]
        public async Task ErrorState_ShowsErrorTextAndRetryButton()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetError();

            var cut = ctx.Render<SubscriptionsPage>();

            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains(ctx.Localizer()["ErrorLoading"].Value, text);
            Assert.Contains(ctx.Localizer()["TryAgain"].Value, text);
            Assert.Empty(cut.FindAll(".mud-progress-circular"));
        }

        [Fact]
        public async Task ErrorState_ShowsTheRealReason_AndHttpStatus()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetError("ErrorServerFailure", 500);

            var cut = ctx.Render<SubscriptionsPage>();

            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains(ctx.Localizer()["ErrorServerFailure"].Value, text);
            Assert.Contains("HTTP 500", text);
        }

        [Fact]
        public async Task EmptyState_ShowsEmptyMessage_AndKeepsFormAvailable()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());

            var cut = ctx.Render<SubscriptionsPage>();

            Assert.Equal(ctx.Localizer()["NoSubscriptionsYet"].Value, cut.Find(".empty-state").TextContent.Trim());
            Assert.Single(cut.FindAll("form"));
            Assert.Equal(2, cut.FindAll("input").Count);
            Assert.NotNull(cut.Find("select"));
        }

        [Fact]
        public async Task SuccessState_ShowsRowForEachSubscription()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(Sample());

            var cut = ctx.Render<SubscriptionsPage>();

            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains("Дрова", text);
            Assert.Contains("Шини", text);
            Assert.Empty(cut.FindAll(".empty-state"));
        }

        [Fact]
        public async Task SubmittingForm_AddsRow_AndReRendersFromEmptyToSuccess()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());
            Seed(ctx, "Овочі");

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("input")[0].Input("user@example.com");
            cut.Find("select").Change("Овочі");

            await cut.Find("form").SubmitAsync();

            cut.WaitForAssertion(() =>
            {
                Assert.Empty(cut.FindAll(".empty-state"));
                Assert.Contains("Овочі", TestHelpers.Text(cut.Markup));
            }, Timeout);
        }

        [Fact]
        public async Task SubmittingEmptyForm_DoesNotAddRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            var state = ctx.AddState<Subscription>();
            state.SetItems(new List<Subscription>());

            var cut = ctx.Render<SubscriptionsPage>();
            await cut.Find("form").SubmitAsync();

            Assert.Empty(state.Items);
            Assert.Single(cut.FindAll(".empty-state"));
        }

        [Theory]
        [InlineData("uk-UA", "3,5")]
        [InlineData("en-US", "3.5")]
        public async Task Threshold_IsShownInRow_WithCultureSpecificSeparator(string culture, string typed)
        {
            using var scope = new CultureScope(culture);
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());
            Seed(ctx, "Овочі");

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("input")[0].Input("user@example.com");
            cut.Find("select").Change("Овочі");
            cut.FindAll("input")[1].Input(typed);

            await cut.Find("form").SubmitAsync();

            cut.WaitForAssertion(
                () => Assert.Contains($"≤ {typed}", TestHelpers.Text(cut.Markup)), Timeout);
        }

        [Fact]
        public async Task TogglingSwitch_UpdatesActiveCount()
        {
            await using var ctx = TestHelpers.CreateContext();
            var items = new List<Subscription>
            {
                new Subscription { Id = 1, Email = "a@example.com", CategoryName = "Дрова" }
            };
            ctx.AddState<Subscription>().SetItems(items);

            var cut = ctx.Render<SubscriptionsPage>();
            Assert.Contains(ctx.Localizer()["SubscriptionsTelegramActive", 1].Value, TestHelpers.Text(cut.Markup));

            cut.Find("input[type=checkbox]").Change(false);

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["SubscriptionsTelegramActive", 0].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.False(items[0].IsActive);
        }

        [Fact]
        public async Task FailedCreate_ShowsLocalizedError_AndDoesNotAddRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            var state = ctx.AddState<Subscription>();
            state.SetItems(new List<Subscription>());
            ctx.Services.GetRequiredService<FakeCatalogueApi>().FailWrites = true;
            Seed(ctx, "Дрова");

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("input")[0].Input("user@example.com");
            cut.Find("select").Change("Дрова");
            await cut.Find("form").SubmitAsync();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["ErrorSubscriptionExists"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Empty(state.Items);
        }

        [Fact]
        public async Task FailedToggle_RevertsSwitch_AndShowsError()
        {
            await using var ctx = TestHelpers.CreateContext();
            var items = new List<Subscription> { new Subscription { Id = 1, Email = "a@example.com", CategoryName = "Дрова" } };
            ctx.AddState<Subscription>().SetItems(items);
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();
            api.FailWrites = true;

            var cut = ctx.Render<SubscriptionsPage>();
            cut.Find("input[type=checkbox]").Change(false);

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["ErrorApiUnavailable"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.True(items[0].IsActive);
            Assert.Equal((1, false), Assert.Single(api.ActiveChanges));
        }

        [Fact]
        public async Task Delete_AfterConfirmation_CallsApi_AndRemovesRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
            var state = ctx.AddState<Subscription>();
            state.SetItems(Sample());
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("button.mud-icon-button")[0].Click();

            cut.WaitForAssertion(() => Assert.Single(state.Items), Timeout);
            Assert.Equal(new[] { 1 }, api.DeletedIds);
            // Перевіряємо email рядка, а не назву категорії: у дефолтній (українській) культурі на Linux-runner
            // «Дрова» є і в статичному тексті сторінки (NextNotificationText), тож перевірка за назвою залежала від культури.
            Assert.DoesNotContain("a@example.com", TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task Delete_WithoutConfirmation_DoesNothing()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<bool>("confirm", _ => true).SetResult(false);
            var state = ctx.AddState<Subscription>();
            state.SetItems(Sample());
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("button.mud-icon-button")[0].Click();

            cut.WaitForAssertion(() => Assert.Equal(2, state.Items.Count), Timeout);
            Assert.Empty(api.DeletedIds);
        }

        [Fact]
        public async Task CategoryDropdown_ListsCategoriesFromTheServer()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());
            Seed(ctx, "Дрова", "Шини");

            var cut = ctx.Render<SubscriptionsPage>();

            var values = cut.FindAll("select option").Select(o => o.GetAttribute("value")).ToList();
            Assert.Equal(new[] { string.Empty, "Дрова", "Шини" }, values);
        }

        [Fact]
        public async Task CategoryFromTheAddress_IsPreselectedInTheForm()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());
            Seed(ctx, "Дрова", "Шини");
            ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("subscriptions?category=2");

            var cut = ctx.Render<SubscriptionsPage>();

            cut.WaitForAssertion(() => Assert.Equal("Шини", cut.Find("select").SelectedValue()), Timeout);
        }

        private static readonly string[] MonthKeys =
            { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        /// <summary>Рік цін товару; ціна залежить від зсуву місяця відносно поточного (0 = цей місяць).</summary>
        private static List<SeasonalPrice> Relative(int productId, int categoryId, Func<int, decimal> priceByOffset) =>
            Enumerable.Range(1, 12).Select(month => new SeasonalPrice
            {
                Id = productId * 100 + month,
                ProductId = productId,
                CategoryId = categoryId,
                Name = "Товар",
                MonthNumber = month,
                Month = month.ToString(),
                Price = priceByOffset((month - DateTime.Today.Month + 12) % 12)
            }).ToList();

        [Fact]
        public async Task EmailField_IsPrefilledWithTheSignedInUsersEmail()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());

            var cut = ctx.Render<SubscriptionsPage>();

            cut.WaitForAssertion(() => Assert.Equal("tester@example.com", cut.FindAll("input")[0].GetAttribute("value")), Timeout);
        }

        [Fact]
        public async Task NextNotification_AsksToSubscribe_WhenThereAreNoSubscriptions()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());

            var cut = ctx.Render<SubscriptionsPage>();

            Assert.Equal("none", cut.Find("[data-outlook]").GetAttribute("data-outlook"));
            Assert.Contains(ctx.Localizer()["NextNotificationNoSubscriptions"].Value, TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task NextNotification_SaysNoDropIsExpected_WhenThePricesAreNotAvailable()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(Sample());

            var cut = ctx.Render<SubscriptionsPage>();

            Assert.Equal("none", cut.Find("[data-outlook]").GetAttribute("data-outlook"));
            Assert.Contains(ctx.Localizer()["NextNotificationNoDrop"].Value, TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task NextNotification_ShowsTheBiggestUpcomingDropAmongTheActiveSubscriptions()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();
            api.Products.Add(new ProductOption(1, 1, "Дрова, дуб"));
            api.Products.Add(new ProductOption(2, 2, "Шини зимові"));
            api.Products.Add(new ProductOption(3, 3, "Цегла"));
            // 33% drop next month
            api.Prices.AddRange(Relative(1, 1, offset => offset switch { 0 => 150m, 1 => 100m, _ => 160m }));
            // 50% drop next month: the biggest one
            api.Prices.AddRange(Relative(2, 2, offset => offset switch { 0 => 200m, 1 => 100m, 6 => 250m, _ => 210m }));
            // 90% drop, but the subscription is switched off, so it must be ignored
            api.Prices.AddRange(Relative(3, 3, offset => offset switch { 0 => 1000m, 1 => 100m, 6 => 1200m, _ => 1050m }));
            ctx.AddState<Subscription>().SetItems(new List<Subscription>
            {
                new() { Id = 1, Email = "a@example.com", CategoryId = 1, CategoryName = "Дрова", IsActive = true },
                new() { Id = 2, Email = "a@example.com", CategoryId = 2, CategoryName = "Шини", IsActive = true },
                new() { Id = 3, Email = "a@example.com", CategoryId = 3, CategoryName = "Цегла", IsActive = false }
            });
            var nextMonthName = ctx.Localizer()["Month_" + MonthKeys[DateTime.Today.Month % 12]].Value;

            var cut = ctx.Render<SubscriptionsPage>();

            cut.WaitForAssertion(() => Assert.Equal("drop", cut.Find("[data-outlook]").GetAttribute("data-outlook")), Timeout);
            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains(ctx.Localizer()["NextNotificationDropText", "Шини зимові", 50].Value, text);
            Assert.Contains(nextMonthName, cut.Find("[data-outlook]").TextContent);
        }
    }
}
