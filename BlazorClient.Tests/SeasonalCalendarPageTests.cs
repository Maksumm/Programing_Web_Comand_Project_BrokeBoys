using BlazorClient.Models;
using BlazorClient.Pages;
using BlazorClient.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class SeasonalCalendarPageTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
        private const string MonthBar = "[style*='width: 7%']";

        private static List<SeasonalPrice> Sample() => new()
        {
            new SeasonalPrice { Id = 1, Name = "Дрова, дуб", Price = 1500m, Month = "Жовтень" }
        };

        [Fact]
        public async Task LoadingState_ShowsSpinnerAndLoadingText()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetLoading();

            var cut = ctx.Render<SeasonalCalendarPage>();

            Assert.Single(cut.FindAll(".mud-progress-circular"));
            Assert.Contains(ctx.Localizer()["LoadingPrices"].Value, TestHelpers.Text(cut.Markup));
            Assert.Empty(cut.FindAll(MonthBar));
        }

        [Fact]
        public async Task ErrorState_ShowsErrorTextAndRetryButton()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetError();

            var cut = ctx.Render<SeasonalCalendarPage>();

            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains(ctx.Localizer()["FailedToLoadData"].Value, text);
            Assert.Contains(ctx.Localizer()["TryAgain"].Value, text);
            Assert.Empty(cut.FindAll(".mud-progress-circular"));
        }

        [Fact]
        public async Task ErrorState_ShowsTheRealReason_AndHttpStatus()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetError("ErrorServerFailure", 500);

            var cut = ctx.Render<SeasonalCalendarPage>();

            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains(ctx.Localizer()["ErrorServerFailure"].Value, text);
            Assert.Contains("HTTP 500", text);
        }

        [Fact]
        public async Task EmptyState_ShowsNothingSelectedMessage()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetItems(new List<SeasonalPrice>());

            var cut = ctx.Render<SeasonalCalendarPage>();

            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains(ctx.Localizer()["NothingSelectedYet"].Value, text);
            Assert.Empty(cut.FindAll(MonthBar));
        }

        [Fact]
        public async Task SuccessState_ShowsTwelveMonthBars()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetItems(Sample());

            var cut = ctx.Render<SeasonalCalendarPage>();

            Assert.Equal(12, cut.FindAll(MonthBar).Count);
            Assert.Contains(ctx.Localizer()["PriceThroughoutYear"].Value, TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task ChangingState_ReRendersPage()
        {
            await using var ctx = TestHelpers.CreateContext();
            var state = ctx.AddState<SeasonalPrice>();
            state.SetLoading();

            var cut = ctx.Render<SeasonalCalendarPage>();
            Assert.Single(cut.FindAll(".mud-progress-circular"));

            state.SetItems(Sample());

            cut.WaitForAssertion(() => Assert.Equal(12, cut.FindAll(MonthBar).Count), Timeout);
            Assert.Empty(cut.FindAll(".mud-progress-circular"));
        }

        [Fact]
        public async Task RealPrices_DriveBarHeights_AndHighlightCheapestMonth()
        {
            await using var ctx = TestHelpers.CreateContext();
            var cheapest = DateTime.Today.Month == 7 ? 8 : 7; // поточний місяць має власний колір
            var prices = Enumerable.Range(1, 12).Select(m => new SeasonalPrice
            {
                Id = m,
                Name = "Дрова, дуб",
                MonthNumber = m,
                Month = m.ToString(),
                Price = m == cheapest ? 40m : m == 12 ? 200m : 100m
            }).ToList();
            ctx.AddState<SeasonalPrice>().SetItems(prices);

            var cut = ctx.Render<SeasonalCalendarPage>();

            Assert.Equal(12, cut.FindAll(MonthBar).Count);
            Assert.Contains("height: 150px", cut.Markup);   // найвища ціна (200)
            Assert.Contains("height: 75px", cut.Markup);    // звичайна ціна (100) — половина найвищої
            Assert.Contains("height: 30px", cut.Markup);    // найнижча ціна (40) — пропорційно
            Assert.Single(cut.FindAll("[style*='background-color: #2D8A57']"));
            Assert.Contains("Дрова, дуб", TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task MonthsWithoutPrice_AreDrawnEmpty()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetItems(new List<SeasonalPrice>
            {
                new SeasonalPrice { Id = 1, Name = "Дрова, дуб", Price = 1500m, MonthNumber = 10, Month = "Жов" }
            });

            var cut = ctx.Render<SeasonalCalendarPage>();

            Assert.Equal(12, cut.FindAll(MonthBar).Count);
            Assert.Equal(11, cut.FindAll("[style*='height: 0px']").Count);
        }

        private static List<SeasonalPrice> Year(int categoryId, decimal julyPrice) =>
            Enumerable.Range(1, 12).Select(m => new SeasonalPrice
            {
                Id = m,
                Name = "Дрова, дуб",
                CategoryId = categoryId,
                MonthNumber = m,
                Month = m.ToString(),
                Price = m == 7 ? julyPrice : 1000m + m
            }).ToList();

        [Fact]
        public async Task JoinsTheLiveGroup_OfTheDisplayedCategory_AndShowsTheLiveBadge()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetItems(Year(categoryId: 3, julyPrice: 1200m));
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();

            var cut = ctx.Render<SeasonalCalendarPage>();

            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-live='connected']")), Timeout);
            Assert.Equal(new[] { 3 }, live.JoinedCategories);
        }

        [Fact]
        public async Task LiveMessage_RefreshesThePrices_WithoutShowingTheLoadingState()
        {
            await using var ctx = TestHelpers.CreateContext();
            var state = ctx.AddState<SeasonalPrice>();
            state.SetItems(Year(categoryId: 1, julyPrice: 1200m));
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();
            api.Prices.AddRange(Year(categoryId: 1, julyPrice: 4242m));

            var cut = ctx.Render<SeasonalCalendarPage>();
            Assert.Contains("1200", TestHelpers.Text(cut.Markup));

            live.Raise(new PriceChanged("Updated", 1, 1, 7, 4242m));

            cut.WaitForAssertion(() => Assert.Contains("4242", TestHelpers.Text(cut.Markup)), Timeout);
            Assert.DoesNotContain("1200", TestHelpers.Text(cut.Markup));
            Assert.Equal(ViewState.Success, state.State);
        }

        private static readonly string[] MonthKeys =
            { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        /// <summary>Рік цін, де ціна залежить від зсуву місяця відносно поточного (0 = цей місяць).</summary>
        private static List<SeasonalPrice> Relative(Func<int, decimal> priceByOffset, int productId = 1, int categoryId = 1, string name = "Дрова, дуб") =>
            Enumerable.Range(1, 12).Select(month => new SeasonalPrice
            {
                Id = productId * 100 + month,
                ProductId = productId,
                CategoryId = categoryId,
                Name = name,
                MonthNumber = month,
                Month = month.ToString(),
                Price = priceByOffset((month - DateTime.Today.Month + 12) % 12)
            }).ToList();

        private static string VerdictKind(Bunit.IRenderedComponent<SeasonalCalendarPage> cut) =>
            cut.Find("[data-verdict]").GetAttribute("data-verdict")!;

        [Fact]
        public async Task Verdict_IsBuyNow_WhenThisMonthIsTheYearlyLow()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetItems(Relative(offset => offset == 0 ? 100m : 200m));

            var cut = ctx.Render<SeasonalCalendarPage>();

            Assert.Equal("Buy", VerdictKind(cut));
            Assert.Contains(ctx.Localizer()["VerdictBuyText", 0].Value, TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task Verdict_IsAvoid_WhenThisMonthIsNearTheYearlyPeak()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>()
               .SetItems(Relative(offset => offset == 0 ? 300m : offset == 3 ? 100m : 200m));

            // 2. Лише ПІСЛЯ цього рендеримо компонент (використовуємо Render замість RenderComponent)
            var cut = ctx.Render<SeasonalCalendarPage>();

            Assert.Equal("Avoid", VerdictKind(cut));
            Assert.Contains(ctx.Localizer()["VerdictAvoidText", 200].Value, TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task Verdict_IsWait_AndNamesTheCheapestUpcomingMonth()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetItems(Relative(offset => offset switch { 0 => 150m, 1 => 100m, 6 => 200m, _ => 160m }));
            var nextMonthName = ctx.Localizer()["Month_" + MonthKeys[DateTime.Today.Month % 12]].Value;

            var cut = ctx.Render<SeasonalCalendarPage>();

            Assert.Equal("Wait", VerdictKind(cut));
            Assert.Contains(ctx.Localizer()["VerdictWaitText", nextMonthName, 33].Value, TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task Verdict_SaysThereIsNotEnoughData_WhenTheCurrentMonthHasNoPrice()
        {
            await using var ctx = TestHelpers.CreateContext();
            var onlyOtherMonths = Relative(offset => 100m + offset).Where(p => p.MonthNumber != DateTime.Today.Month).ToList();
            ctx.AddState<SeasonalPrice>().SetItems(onlyOtherMonths);

            var cut = ctx.Render<SeasonalCalendarPage>();

            Assert.Equal("None", VerdictKind(cut));
            Assert.Contains(ctx.Localizer()["VerdictNoData"].Value, TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task ProductPicker_IsHidden_WhenThereAreNoProducts()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetItems(Year(categoryId: 1, julyPrice: 1200m));

            var cut = ctx.Render<SeasonalCalendarPage>();

            Assert.Empty(cut.FindAll("select"));
        }

        [Fact]
        public async Task ProductPicker_ListsProducts_AndSelectingOneLoadsItsPrices()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();
            api.Products.Add(new ProductOption(1, 1, "Дрова, дуб"));
            api.Products.Add(new ProductOption(2, 2, "Шини зимові"));
            api.Prices.AddRange(Relative(_ => 500m, productId: 2, categoryId: 2, name: "Шини зимові"));
            ctx.AddState<SeasonalPrice>().SetItems(Relative(_ => 100m));
            var live = ctx.Services.GetRequiredService<FakeLiveUpdates>();

            var cut = ctx.Render<SeasonalCalendarPage>();

            cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("select option").Count), Timeout);
            Assert.Equal("1", cut.Find("select").SelectedValue());

            cut.Find("select").Change("2");

            cut.WaitForAssertion(() => Assert.Contains("Шини зимові", cut.Find("h4").TextContent), Timeout);
            Assert.Equal(new[] { 2 }, api.RequestedProductIds);
            Assert.Contains(2, live.JoinedCategories);
            Assert.Contains("500", TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task EmptyState_OffersTheRealProductsAsChoices()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();
            api.Products.Add(new ProductOption(7, 1, "Цегла"));
            ctx.AddState<SeasonalPrice>().SetItems(new List<SeasonalPrice>());

            var cut = ctx.Render<SeasonalCalendarPage>();

            cut.WaitForAssertion(() => Assert.Contains("Цегла", TestHelpers.Text(cut.Markup)), Timeout);
            Assert.DoesNotContain(ctx.Localizer()["Tires"].Value, TestHelpers.Text(cut.Markup));
        }

        private static void WaitForEmail(Bunit.IRenderedComponent<SeasonalCalendarPage> cut, string expected) =>
            cut.WaitForAssertion(() => Assert.Equal(expected, cut.Find("input").GetAttribute("value")), Timeout);

        [Fact]
        public async Task SubscribeCard_PrefillsTheEmailOfTheSignedInUser()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<SeasonalPrice>().SetItems(Year(categoryId: 3, julyPrice: 1200m));

            var cut = ctx.Render<SeasonalCalendarPage>();

            WaitForEmail(cut, "tester@example.com");
        }

        [Fact]
        public async Task Subscribe_SubscribesToTheCategoryOfTheShownProduct_AndConfirms()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();
            ctx.AddState<SeasonalPrice>().SetItems(Year(categoryId: 3, julyPrice: 1200m));

            var cut = ctx.Render<SeasonalCalendarPage>();
            WaitForEmail(cut, "tester@example.com");
            cut.Find("#calendar-subscribe").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["SubscribedToCategory"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Equal((3, "tester@example.com", (decimal?)null), Assert.Single(api.CategorySubscriptions));
        }

        [Fact]
        public async Task Subscribe_WithAnInvalidEmail_ShowsTheMessage_AndDoesNotCallTheApi()
        {
            await using var ctx = TestHelpers.CreateContext();
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();
            ctx.AddState<SeasonalPrice>().SetItems(Year(categoryId: 3, julyPrice: 1200m));

            var cut = ctx.Render<SeasonalCalendarPage>();
            WaitForEmail(cut, "tester@example.com");
            cut.Find("input").Input("not-an-email");
            cut.Find("#calendar-subscribe").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["EmailInvalid"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Empty(api.CategorySubscriptions);
        }

        [Fact]
        public async Task Subscribe_WhenTheServerRefuses_ShowsTheReason()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.Services.GetRequiredService<FakeCatalogueApi>().FailWrites = true;
            ctx.AddState<SeasonalPrice>().SetItems(Year(categoryId: 3, julyPrice: 1200m));

            var cut = ctx.Render<SeasonalCalendarPage>();
            WaitForEmail(cut, "tester@example.com");
            cut.Find("#calendar-subscribe").Click();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["ErrorSubscriptionExists"].Value, TestHelpers.Text(cut.Markup)), Timeout);
        }
    }
}
