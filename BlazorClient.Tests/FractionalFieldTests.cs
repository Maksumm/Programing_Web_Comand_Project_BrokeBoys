using BlazorClient.Components;
using BlazorClient.Models;
using BlazorClient.Pages;
using BlazorClient.Resources;
using Bunit;
using MudBlazor;

namespace BlazorClient.Tests;

public class FractionalFieldTests
{
    private static async Task FillRequiredFields(IRenderedComponent<SubscribeFormComponent> cut)
    {
        var fields = cut.FindComponents<MudTextField<string>>();
        await fields[0].Find("input").InputAsync("user@example.com");
        await fields[1].Find("input").InputAsync("Дрова");
    }

    // Головна перевірка з ТЗ: поле з дробовим числом приймає «3,5» для uk-UA і «3.5» для en-US
    [Theory]
    [InlineData("uk-UA", "3,5")]
    [InlineData("en-US", "3.5")]
    public async Task NumericField_AcceptsFraction_ForCulture(string culture, string input)
    {
        await using var ctx = TestHelpers.CreateContext(culture);
        SubscribeFormModel? submitted = null;
        var cut = ctx.Render<SubscribeFormComponent>(p => p
            .Add(c => c.OnSubmitted, (SubscribeFormModel m) => submitted = m));
        await FillRequiredFields(cut);

        await cut.FindComponent<MudNumericField<decimal?>>().Find("input").InputAsync(input);
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(submitted);
            Assert.Equal(3.5m, submitted!.Threshold);
        });
    }

    [Fact]
    public async Task Threshold_IsOptional()
    {
        await using var ctx = TestHelpers.CreateContext();
        SubscribeFormModel? submitted = null;
        var cut = ctx.Render<SubscribeFormComponent>(p => p
            .Add(c => c.OnSubmitted, (SubscribeFormModel m) => submitted = m));
        await FillRequiredFields(cut);

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(submitted);
            Assert.Null(submitted!.Threshold);
        });
    }

    [Fact]
    public async Task Threshold_OutOfRange_ShowsMessage_AndDoesNotSubmit()
    {
        await using var ctx = TestHelpers.CreateContext("uk-UA");
        var submitted = 0;
        var cut = ctx.Render<SubscribeFormComponent>(p => p
            .Add(c => c.OnSubmitted, (SubscribeFormModel _) => submitted++));
        await FillRequiredFields(cut);

        var numeric = cut.FindComponent<MudNumericField<decimal?>>();
        await numeric.Find("input").InputAsync("5000");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Contains(AppResources.ThresholdRange, numeric.Markup));
        Assert.Equal(0, submitted);
    }

    // Те саме значення на екрані підписок: десятковий роздільник залежить від культури
    [Theory]
    [InlineData("uk-UA", "3,5", "≤ 3,5")]
    [InlineData("en-US", "3.5", "≤ 3.5")]
    public async Task SubscriptionsPage_ShowsThreshold_InCultureFormat(string culture, string input, string expected)
    {
        await using var ctx = TestHelpers.CreateContext(culture);
        var cut = ctx.Render<SubscriptionsPage>();
        var fields = cut.FindComponents<MudTextField<string>>();
        await fields[0].Find("input").InputAsync("user@example.com");
        await fields[1].Find("input").InputAsync("Дрова");
        await cut.FindComponent<MudNumericField<decimal?>>().Find("input").InputAsync(input);

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() =>
            Assert.Contains(expected, cut.Find(".subscription-row").TextContent));
    }
}
