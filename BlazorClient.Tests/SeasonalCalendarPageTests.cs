using BlazorClient.Models;
using BlazorClient.Pages;
using BlazorClient.Resources;
using Bunit;

namespace BlazorClient.Tests;

public class SeasonalCalendarPageTests
{
    [Fact]
    public async Task Loading_ShowsSpinnerAndText_WithoutChart()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<SeasonalCalendarPage>(p => p.Add(c => c.IsLoading, true));

        Assert.NotEmpty(cut.FindAll(".mud-progress-circular"));
        Assert.Contains(AppResources.LoadingPrices, cut.Markup);
        Assert.Empty(cut.FindAll(".month-column"));
    }

    [Fact]
    public async Task Error_ShowsMessage_AndRetryInvokesCallback()
    {
        await using var ctx = TestHelpers.CreateContext();
        var retried = 0;

        var cut = ctx.Render<SeasonalCalendarPage>(p => p
            .Add(c => c.HasError, true)
            .Add(c => c.OnRetry, () => retried++));

        Assert.Contains(AppResources.FailedToLoadData, cut.Markup);
        Assert.Empty(cut.FindAll(".month-column"));

        await cut.Find(".retry-button").ClickAsync();

        Assert.Equal(1, retried);
    }

    [Fact]
    public async Task Empty_ShowsPlaceholder_NotChart()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<SeasonalCalendarPage>(p => p.Add(c => c.Prices, new List<SeasonalPrice>()));

        Assert.Contains(AppResources.NothingSelectedYet, cut.Markup);
        Assert.Empty(cut.FindAll(".month-column"));
    }

    [Fact]
    public async Task NoParameters_IsEmptyState()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<SeasonalCalendarPage>();

        Assert.Contains(AppResources.NothingSelectedYet, cut.Markup);
    }

    [Fact]
    public async Task Data_ShowsTwelveMonthColumns_AndTitle()
    {
        await using var ctx = TestHelpers.CreateContext();
        var items = new List<SeasonalPrice>
        {
            new() { Id = 1, Name = "Дрова, дуб", Price = 1500m, Month = "Жовтень" }
        };

        var cut = ctx.Render<SeasonalCalendarPage>(p => p.Add(c => c.Prices, items));

        Assert.Equal(12, cut.FindAll(".month-column").Count);
        Assert.Contains(AppResources.FirewoodOakTitle, cut.Markup);
        Assert.DoesNotContain(AppResources.NothingSelectedYet, cut.Markup);
    }
}
