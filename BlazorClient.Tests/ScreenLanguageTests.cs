using System.Globalization;
using BlazorClient.Models;
using BlazorClient.Pages;
using BlazorClient.Resources;
using Bunit;

namespace BlazorClient.Tests;

/// <summary>Той самий екран двома мовами: заголовки й підписи беруться з .resx обраної культури.</summary>
public class ScreenLanguageTests
{
    private static string Res(string key, string culture) =>
        AppResources.ResourceManager.GetString(key, new CultureInfo(culture))!;

    [Theory]
    [InlineData("uk-UA")]
    [InlineData("en-US")]
    public async Task CategoriesPage_ShowsTexts_InSelectedLanguage(string culture)
    {
        await using var ctx = TestHelpers.CreateContext(culture);

        var cut = ctx.Render<CategoriesPage>(p => p.Add(c => c.Categories, new List<Category>()));

        Assert.Contains(Res("CategoriesTitle", culture), cut.Markup);
        Assert.Contains(Res("CategoriesYetEmpty", culture), cut.Markup);
    }

    [Theory]
    [InlineData("uk-UA")]
    [InlineData("en-US")]
    public async Task CalendarPage_ShowsTexts_InSelectedLanguage(string culture)
    {
        await using var ctx = TestHelpers.CreateContext(culture);
        var items = new List<SeasonalPrice> { new() { Id = 1, Name = "x", Price = 1m, Month = "m" } };

        var cut = ctx.Render<SeasonalCalendarPage>(p => p.Add(c => c.Prices, items));

        Assert.Contains(Res("FirewoodOakTitle", culture), cut.Markup);
        Assert.Contains(Res("PriceThroughoutYear", culture), cut.Markup);
        Assert.Contains(Res("Month_Jan", culture), cut.Markup);
    }
}
