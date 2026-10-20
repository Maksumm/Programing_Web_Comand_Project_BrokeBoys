using BlazorClient.Components;
using BlazorClient.Models;
using BlazorClient.Resources;
using Bunit;
using MudBlazor;

namespace BlazorClient.Tests;

public class SubscribeFormTests
{
    // Поля форми у порядку розмітки: [0] — e-mail, [1] — категорія
    private static IReadOnlyList<IRenderedComponent<MudTextField<string>>> TextFields(
        IRenderedComponent<SubscribeFormComponent> cut) => cut.FindComponents<MudTextField<string>>();

    [Fact]
    public async Task EmptySubmit_ShowsMessageUnderEachRequiredField_AndDoesNotInvokeCallback()
    {
        await using var ctx = TestHelpers.CreateContext();
        var submitted = 0;
        var cut = ctx.Render<SubscribeFormComponent>(p => p
            .Add(c => c.OnSubmitted, (SubscribeFormModel _) => submitted++));

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() =>
        {
            var fields = TextFields(cut);
            Assert.Contains(AppResources.EmailRequired, fields[0].Markup);
            Assert.Contains(AppResources.CategoryRequired, fields[1].Markup);
            // повідомлення стоять під «своїм» полем, а не під чужим
            Assert.DoesNotContain(AppResources.CategoryRequired, fields[0].Markup);
            Assert.DoesNotContain(AppResources.EmailRequired, fields[1].Markup);
        });
        Assert.Equal(0, submitted);
    }

    [Fact]
    public async Task InvalidEmail_ShowsFormatMessage()
    {
        await using var ctx = TestHelpers.CreateContext();
        var cut = ctx.Render<SubscribeFormComponent>();
        var fields = TextFields(cut);
        await fields[0].Find("input").InputAsync("not-an-email");
        await fields[1].Find("input").InputAsync("Дрова");

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() =>
            Assert.Contains(AppResources.EmailInvalid, TextFields(cut)[0].Markup));
    }

    [Fact]
    public async Task ValidSubmit_InvokesCallback_WithEnteredValues()
    {
        await using var ctx = TestHelpers.CreateContext();
        SubscribeFormModel? submitted = null;
        var cut = ctx.Render<SubscribeFormComponent>(p => p
            .Add(c => c.OnSubmitted, (SubscribeFormModel m) => submitted = m));
        var fields = TextFields(cut);
        await fields[0].Find("input").InputAsync("user@example.com");
        await fields[1].Find("input").InputAsync("Дрова");

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(submitted);
            Assert.Equal("user@example.com", submitted!.Email);
            Assert.Equal("Дрова", submitted.Category);
        });
    }

    [Fact]
    public async Task ValidSubmit_ClearsFields()
    {
        await using var ctx = TestHelpers.CreateContext();
        var cut = ctx.Render<SubscribeFormComponent>();
        var fields = TextFields(cut);
        await fields[0].Find("input").InputAsync("user@example.com");
        await fields[1].Find("input").InputAsync("Дрова");

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(string.Empty, TextFields(cut)[0].Find("input").GetAttribute("value") ?? string.Empty);
            Assert.Equal(string.Empty, TextFields(cut)[1].Find("input").GetAttribute("value") ?? string.Empty);
        });
    }
}
