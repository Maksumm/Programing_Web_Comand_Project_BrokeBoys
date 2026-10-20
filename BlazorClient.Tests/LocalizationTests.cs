using System.Globalization;
using System.Text.RegularExpressions;
using BlazorClient.Layout;
using BlazorClient.Pages;
using BlazorClient.Resources;
using BlazorClient.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorClient.Tests;

public class LocalizationTests
{
    private static ISet<string> Keys(CultureInfo culture, bool tryParents)
    {
        var set = AppResources.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: tryParents);
        Assert.NotNull(set);
        return set!.Cast<System.Collections.DictionaryEntry>().Select(e => (string)e.Key).ToHashSet();
    }

    [Fact]
    public void EnglishResources_ContainEveryKeyOfTheDefaultResources()
    {
        var all = Keys(CultureInfo.InvariantCulture, tryParents: false);
        var english = Keys(new CultureInfo("en-US"), tryParents: false);

        Assert.Empty(all.Except(english));
    }

    [Fact]
    public void UkrainianResources_ContainEveryKeyOfTheDefaultResources()
    {
        var all = Keys(CultureInfo.InvariantCulture, tryParents: false);
        var ukrainian = Keys(new CultureInfo("uk-UA"), tryParents: false);

        Assert.Empty(all.Except(ukrainian));
    }

    [Theory]
    [InlineData("MySubscriptions")]
    [InlineData("SubscriptionsEmpty")]
    [InlineData("EmailRequired")]
    [InlineData("ThresholdRange")]
    [InlineData("NotFound")]
    public void Key_HasDifferentTextForUkrainianAndEnglish(string key)
    {
        var uk = AppResources.ResourceManager.GetString(key, new CultureInfo("uk-UA"));
        var en = AppResources.ResourceManager.GetString(key, new CultureInfo("en-US"));

        Assert.False(string.IsNullOrWhiteSpace(uk));
        Assert.False(string.IsNullOrWhiteSpace(en));
        Assert.NotEqual(uk, en);
    }

    [Theory]
    [InlineData("uk-UA", "Мої підписки")]
    [InlineData("en-US", "My subscriptions")]
    public async Task SubscriptionsPage_UsesSelectedCulture(string culture, string expectedTitle)
    {
        await using var ctx = TestHelpers.CreateContext(culture);

        var cut = ctx.Render<SubscriptionsPage>();

        Assert.Contains(expectedTitle, cut.Markup);
    }

    [Fact]
    public async Task CultureService_PersistsChoice_AndReloads()
    {
        await using var ctx = TestHelpers.CreateContext();
        var service = ctx.Services.GetRequiredService<CultureService>();

        await service.ChangeCultureAsync("en-US");

        var save = Assert.Single(ctx.JSInterop.Invocations["localStorage.setItem"]);
        Assert.Equal("client_culture", save.Arguments[0]);
        Assert.Equal("en-US", save.Arguments[1]);
        Assert.Single(ctx.JSInterop.Invocations["location.reload"]);
    }

    [Fact]
    public async Task LanguageButton_SavesChoice_ThroughCultureService()
    {
        await using var ctx = TestHelpers.CreateContext();
        var cut = ctx.Render<MainLayout>(p => p.Add(c => c.Body, "<p>content</p>"));

        var enButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "EN");
        await enButton.ClickAsync();

        var save = Assert.Single(ctx.JSInterop.Invocations["localStorage.setItem"]);
        Assert.Equal("en-US", save.Arguments[1]);
        Assert.Single(ctx.JSInterop.Invocations["location.reload"]);
    }

    // Захист від регресу: у розмітці не має лишатися кириличних рядків — усе через .resx
    [Fact]
    public void RazorFiles_ContainNoHardcodedUkrainianText()
    {
        var root = FindRepoRoot();
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "BlazorClient"), "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            text = Regex.Replace(text, @"<!--.*?-->", "", RegexOptions.Singleline);   // HTML-коментарі
            text = Regex.Replace(text, @"@\*.*?\*@", "", RegexOptions.Singleline);    // Razor-коментарі
            text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);    // C# блокові коментарі
            text = Regex.Replace(text, @"(^|\s)//[^\n]*", "", RegexOptions.Multiline); // C# рядкові коментарі

            if (Regex.IsMatch(text, "[А-Яа-яІіЇїЄєҐґ]"))
            {
                offenders.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.True(offenders.Count == 0, "Захардкоджений текст у: " + string.Join(", ", offenders));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BlazorApp.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Не знайдено BlazorApp.sln");
    }
}
