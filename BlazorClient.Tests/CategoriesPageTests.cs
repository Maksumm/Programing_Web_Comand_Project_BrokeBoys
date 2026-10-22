using BlazorClient.Models;
using BlazorClient.Pages;
using BlazorClient.Resources;
using Bunit;

namespace BlazorClient.Tests;

public class CategoriesPageTests
{
    [Fact]
    public async Task Loading_ShowsSpinner_AndNoCards()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<CategoriesPage>(p => p.Add(c => c.IsLoading, true));

        Assert.NotEmpty(cut.FindAll(".mud-progress-circular"));
        Assert.Empty(cut.FindAll(".mud-card"));
        Assert.Empty(cut.FindAll(".error-state"));
    }

    [Fact]
    public async Task Error_ShowsErrorMessage()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<CategoriesPage>(p => p.Add(c => c.HasError, true));

        Assert.Contains(AppResources.ErrorLoadingCategories, cut.Find(".error-state").TextContent);
        Assert.Empty(cut.FindAll(".mud-card"));
    }

    [Fact]
    public async Task Empty_ShowsEmptyMessage()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<CategoriesPage>(p => p.Add(c => c.Categories, new List<Category>()));

        Assert.Contains(AppResources.CategoriesYetEmpty, cut.Find(".empty-state").TextContent);
        Assert.Empty(cut.FindAll(".mud-card"));
    }

    [Fact]
    public async Task NullCategories_AreTreatedAsEmpty()
    {
        await using var ctx = TestHelpers.CreateContext();

        var cut = ctx.Render<CategoriesPage>();

        Assert.NotEmpty(cut.FindAll(".empty-state"));
    }

    [Fact]
    public async Task Data_RendersCardPerCategory_WithNameAndCount()
    {
        await using var ctx = TestHelpers.CreateContext();
        var items = new List<Category>
        {
            new() { Id = 1, Name = "Овочі", Description = "Сезонні овочі", SubscriptionsCount = 3 },
            new() { Id = 2, Name = "Фрукти", Description = "Сезонні фрукти", SubscriptionsCount = 5 }
        };

        var cut = ctx.Render<CategoriesPage>(p => p.Add(c => c.Categories, items));

        var cards = cut.FindAll(".mud-card");
        Assert.Equal(2, cards.Count);
        Assert.Contains("Овочі", cards[0].TextContent);
        Assert.Contains("3", cards[0].TextContent);
        Assert.Contains("Фрукти", cards[1].TextContent);
        Assert.Contains("5", cards[1].TextContent);
        Assert.Empty(cut.FindAll(".empty-state"));
    }
}
