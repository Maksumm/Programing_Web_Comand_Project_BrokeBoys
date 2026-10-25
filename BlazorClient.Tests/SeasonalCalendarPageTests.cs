using BlazorClient.Models;
using BlazorClient.Pages;
using Bunit;
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
    }
}
