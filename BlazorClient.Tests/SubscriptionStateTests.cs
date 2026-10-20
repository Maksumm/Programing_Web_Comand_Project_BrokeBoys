using BlazorClient.Services;

namespace BlazorClient.Tests;

public class SubscriptionStateTests
{
    [Fact]
    public void Add_AppendsItem_AndRaisesChanged()
    {
        var state = new SubscriptionState();
        var raised = 0;
        state.Changed += () => raised++;

        state.Add("a@b.com", "Дрова");

        var item = Assert.Single(state.Items);
        Assert.Equal("a@b.com", item.Email);
        Assert.Equal("Дрова", item.CategoryName);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Add_AssignsUniqueIds()
    {
        var state = new SubscriptionState();

        state.Add("a@b.com", "Дрова");
        state.Add("c@d.com", "Шини");

        Assert.Equal(2, state.Items.Select(i => i.Id).Distinct().Count());
    }

    [Fact]
    public void Add_StoresThreshold()
    {
        var state = new SubscriptionState();

        state.Add("a@b.com", "Дрова", 3.5m);

        Assert.Equal(3.5m, state.Items[0].Threshold);
    }

    [Fact]
    public void Remove_DeletesItem_AndRaisesChanged()
    {
        var state = new SubscriptionState();
        state.Add("a@b.com", "Дрова");
        var id = state.Items[0].Id;
        var raised = 0;
        state.Changed += () => raised++;

        state.Remove(id);

        Assert.Empty(state.Items);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Remove_UnknownId_DoesNothing()
    {
        var state = new SubscriptionState();
        state.Add("a@b.com", "Дрова");
        var raised = 0;
        state.Changed += () => raised++;

        state.Remove(999);

        Assert.Single(state.Items);
        Assert.Equal(0, raised);
    }
}
