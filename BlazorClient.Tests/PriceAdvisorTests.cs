using BlazorClient.Services;
using Xunit;

namespace BlazorClient.Tests
{
    /// <summary>Спільна логіка поради: календар і екран підписок мають давати однакову відповідь на однакові ціни.</summary>
    public class PriceAdvisorTests
    {
        private static Dictionary<int, decimal> Year(Func<int, decimal> price) =>
            Enumerable.Range(1, 12).ToDictionary(month => month, price);

        [Fact]
        public void FewerThanTwoPrices_GiveNoAdvice()
        {
            var advice = PriceAdvisor.Advise(new Dictionary<int, decimal> { [1] = 100m }, 1);

            Assert.Equal(AdviceKind.None, advice.Kind);
            Assert.Equal("VerdictNoData", advice.TextKey);
        }

        [Fact]
        public void MissingPriceForTheCurrentMonth_GivesNoAdvice()
        {
            var advice = PriceAdvisor.Advise(Year(month => 100m + month).Where(p => p.Key != 5).ToDictionary(p => p.Key, p => p.Value), 5);

            Assert.Equal(AdviceKind.None, advice.Kind);
        }

        [Fact]
        public void ZeroPriceForTheCurrentMonth_GivesNoAdvice()
        {
            var advice = PriceAdvisor.Advise(Year(month => month == 5 ? 0m : 100m), 5);

            Assert.Equal(AdviceKind.None, advice.Kind);
        }

        [Fact]
        public void AtTheYearlyLow_IsBuy_WithZeroPercentAboveTheMinimum()
        {
            var advice = PriceAdvisor.Advise(Year(month => month == 3 ? 100m : 200m), 3);

            Assert.Equal(AdviceKind.Buy, advice.Kind);
            Assert.Equal("VerdictBuyText", advice.TextKey);
            Assert.Equal(0, advice.Percent);
        }

        [Fact]
        public void WithinFivePercentOfTheLow_IsStillBuy_AndSaysHowFarAboveItIs()
        {
            var advice = PriceAdvisor.Advise(Year(month => month == 3 ? 104m : month == 5 ? 100m : 200m), 3);

            Assert.Equal(AdviceKind.Buy, advice.Kind);
            Assert.Equal(4, advice.Percent);
        }

        [Fact]
        public void NearTheYearlyPeak_IsAvoid()
        {
            var advice = PriceAdvisor.Advise(Year(month => month == 3 ? 300m : month == 6 ? 100m : 200m), 3);

            Assert.Equal(AdviceKind.Avoid, advice.Kind);
            Assert.Equal("VerdictAvoidText", advice.TextKey);
            Assert.Equal(200, advice.Percent);
        }

        [Fact]
        public void InBetween_WaitsForTheCheapestUpcomingMonth()
        {
            var advice = PriceAdvisor.Advise(Year(month => month switch { 10 => 150m, 11 => 140m, 2 => 100m, 6 => 200m, _ => 160m }), 10);

            Assert.Equal(AdviceKind.Wait, advice.Kind);
            Assert.Equal("VerdictWaitText", advice.TextKey);
            Assert.Equal(2, advice.Month);
            Assert.Equal(33, advice.Percent);
        }

        [Fact]
        public void TheSearchWrapsPastDecember()
        {
            var advice = PriceAdvisor.Advise(Year(month => month switch { 12 => 150m, 1 => 100m, _ => 160m }), 12);

            Assert.Equal(AdviceKind.Wait, advice.Kind);
            Assert.Equal(1, advice.Month);
        }

        [Fact]
        public void WhenSeveralMonthsAreEquallyCheap_TheEarliestUpcomingOneWins()
        {
            var advice = PriceAdvisor.Advise(Year(month => month switch { 10 => 200m, 11 => 100m, 12 => 100m, 5 => 250m, _ => 210m }), 10);

            Assert.Equal(AdviceKind.Wait, advice.Kind);
            Assert.Equal(11, advice.Month);
        }

        [Fact]
        public void WhenNothingIsMeaningfullyCheaperLater_ItIsStableBuy()
        {
            // 95.2 is only 4.8% below 100: not worth waiting, yet 100 is not within 5% of that low either
            var advice = PriceAdvisor.Advise(Year(month => month switch { 5 => 100m, 8 => 95.2m, 1 => 120m, _ => 110m }), 5);

            Assert.Equal(AdviceKind.Buy, advice.Kind);
            Assert.Equal("VerdictStableText", advice.TextKey);
            Assert.Null(advice.Month);
        }

        [Fact]
        public void TwoPricesAreEnoughToAdvise()
        {
            var advice = PriceAdvisor.Advise(new Dictionary<int, decimal> { [1] = 100m, [2] = 200m }, 2);

            Assert.Equal(AdviceKind.Avoid, advice.Kind);
        }
    }
}
