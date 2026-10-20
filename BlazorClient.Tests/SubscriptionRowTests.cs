using BlazorClient.Components;
using BlazorClient.Models;
using Bunit;

namespace BlazorClient.Tests;

public class SubscriptionRowTests
{
    private static Subscription Sample(decimal? threshold = null) => new()
    {
        Id = 7,
        Email = "user@example.com",
        CategoryName = "Дрова",
        Threshold = threshold
    };

    [Fact]
    public async Task Renders_EmailAndCategory()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<SubscriptionRow>(p => p.Add(c => c.Item, Sample()));

        Assert.Contains("user@example.com", cut.Markup);
        Assert.Contains("Дрова", cut.Markup);
    }

    [Fact]
    public async Task RemoveClick_InvokesCallback_WithItemId()
    {
        await using var ctx = TestHelpers.CreateContext();
        int? removedId = null;

        var cut = ctx.Render<SubscriptionRow>(p => p
            .Add(c => c.Item, Sample())
            .Add(c => c.OnRemove, (int id) => removedId = id));

        await cut.Find("button").ClickAsync();

        Assert.Equal(7, removedId);
    }
}
