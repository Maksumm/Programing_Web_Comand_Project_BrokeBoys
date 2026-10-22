using BlazorClient.Pages;
using BlazorClient.Resources;
using BlazorClient.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorClient.Tests;

public class SubscriptionsPageTests
{
    [Fact]
    public async Task Loading_ShowsSpinner_AndHidesContent()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<SubscriptionsPage>(p => p.Add(c => c.IsLoading, true));

        Assert.NotEmpty(cut.FindAll(".mud-progress-circular"));
        Assert.Contains(AppResources.LoadingSubscriptions, cut.Markup);
        Assert.Empty(cut.FindAll("form"));
        Assert.Empty(cut.FindAll(".subscription-row"));
    }

    [Fact]
    public async Task Error_ShowsMessage_AndRetryInvokesCallback()
    {
        await using var ctx = TestHelpers.CreateContext();
        var retried = 0;

        var cut = ctx.Render<SubscriptionsPage>(p => p
            .Add(c => c.HasError, true)
            .Add(c => c.OnRetry, () => retried++));

        Assert.NotEmpty(cut.FindAll(".error-state"));
        Assert.Contains(AppResources.ErrorLoading, cut.Markup);

        await cut.Find(".retry-button").ClickAsync();

        Assert.Equal(1, retried);
    }

    [Fact]
    public async Task Empty_ShowsEmptyMessage_AndStillOffersForm()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<SubscriptionsPage>();

        Assert.NotEmpty(cut.FindAll(".empty-state"));
        Assert.Contains(AppResources.SubscriptionsEmpty, cut.Markup);
        Assert.Empty(cut.FindAll(".subscription-row"));
        Assert.NotNull(cut.Find("form"));
    }

    [Fact]
    public async Task Data_RendersOneRowPerSubscription()
    {
        await using var ctx = TestHelpers.CreateContext();
        var state = ctx.Services.GetRequiredService<SubscriptionState>();
        state.Add("a@b.com", "Дрова");
        state.Add("c@d.com", "Шини");

        var cut = ctx.Render<SubscriptionsPage>();

        Assert.Equal(2, cut.FindAll(".subscription-row").Count);
        Assert.Empty(cut.FindAll(".empty-state"));
    }

    [Fact]
    public async Task AddingToState_ReRendersList()
    {
        await using var ctx = TestHelpers.CreateContext();
        var state = ctx.Services.GetRequiredService<SubscriptionState>();
        var cut = ctx.Render<SubscriptionsPage>();
        Assert.Empty(cut.FindAll(".subscription-row"));

        state.Add("new@user.com", "Дрова");

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".subscription-row"));
            Assert.Contains("new@user.com", cut.Markup);
            Assert.Empty(cut.FindAll(".empty-state"));
        });
    }

    [Fact]
    public async Task RemoveButton_RemovesRow_AndShowsEmptyState()
    {
        await using var ctx = TestHelpers.CreateContext();
        var state = ctx.Services.GetRequiredService<SubscriptionState>();
        state.Add("a@b.com", "Дрова");
        var cut = ctx.Render<SubscriptionsPage>();

        await cut.Find(".subscription-row button").ClickAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(state.Items);
            Assert.Empty(cut.FindAll(".subscription-row"));
            Assert.NotEmpty(cut.FindAll(".empty-state"));
        });
    }
}
