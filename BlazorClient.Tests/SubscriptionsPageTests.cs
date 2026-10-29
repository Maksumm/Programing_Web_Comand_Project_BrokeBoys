using BlazorClient.Models;
using BlazorClient.Pages;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class SubscriptionsPageTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        private static List<Subscription> Sample() => new()
        {
            new Subscription { Id = 1, Email = "a@example.com", CategoryName = "Дрова", Threshold = 90m },
            new Subscription { Id = 2, Email = "b@example.com", CategoryName = "Шини" }
        };

        [Fact]
        public async Task LoadingState_ShowsSpinnerAndLoadingText()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetLoading();

            var cut = ctx.Render<SubscriptionsPage>();

            Assert.Single(cut.FindAll(".mud-progress-circular"));
            Assert.Contains(ctx.Localizer()["LoadingSubscriptions"].Value, TestHelpers.Text(cut.Markup));
            Assert.Empty(cut.FindAll("form"));
        }

        [Fact]
        public async Task ErrorState_ShowsErrorTextAndRetryButton()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetError();

            var cut = ctx.Render<SubscriptionsPage>();

            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains(ctx.Localizer()["ErrorLoading"].Value, text);
            Assert.Contains(ctx.Localizer()["TryAgain"].Value, text);
            Assert.Empty(cut.FindAll(".mud-progress-circular"));
        }

        [Fact]
        public async Task ErrorState_ShowsTheRealReason_AndHttpStatus()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetError("ErrorServerFailure", 500);

            var cut = ctx.Render<SubscriptionsPage>();

            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains(ctx.Localizer()["ErrorServerFailure"].Value, text);
            Assert.Contains("HTTP 500", text);
        }

        [Fact]
        public async Task EmptyState_ShowsEmptyMessage_AndKeepsFormAvailable()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());

            var cut = ctx.Render<SubscriptionsPage>();

            Assert.Equal(ctx.Localizer()["NoSubscriptionsYet"].Value, cut.Find(".empty-state").TextContent.Trim());
            Assert.Single(cut.FindAll("form"));
            Assert.Equal(3, cut.FindAll("input").Count);
        }

        [Fact]
        public async Task SuccessState_ShowsRowForEachSubscription()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(Sample());

            var cut = ctx.Render<SubscriptionsPage>();

            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains("Дрова", text);
            Assert.Contains("Шини", text);
            Assert.Empty(cut.FindAll(".empty-state"));
        }

        [Fact]
        public async Task SubmittingForm_AddsRow_AndReRendersFromEmptyToSuccess()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("input")[0].Input("user@example.com");
            cut.FindAll("input")[1].Input("Овочі");

            await cut.Find("form").SubmitAsync();

            cut.WaitForAssertion(() =>
            {
                Assert.Empty(cut.FindAll(".empty-state"));
                Assert.Contains("Овочі", TestHelpers.Text(cut.Markup));
            }, Timeout);
        }

        [Fact]
        public async Task SubmittingEmptyForm_DoesNotAddRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            var state = ctx.AddState<Subscription>();
            state.SetItems(new List<Subscription>());

            var cut = ctx.Render<SubscriptionsPage>();
            await cut.Find("form").SubmitAsync();

            Assert.Empty(state.Items);
            Assert.Single(cut.FindAll(".empty-state"));
        }

        [Theory]
        [InlineData("uk-UA", "3,5")]
        [InlineData("en-US", "3.5")]
        public async Task Threshold_IsShownInRow_WithCultureSpecificSeparator(string culture, string typed)
        {
            using var scope = new CultureScope(culture);
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Subscription>().SetItems(new List<Subscription>());

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("input")[0].Input("user@example.com");
            cut.FindAll("input")[1].Input("Овочі");
            cut.FindAll("input")[2].Input(typed);

            await cut.Find("form").SubmitAsync();

            cut.WaitForAssertion(
                () => Assert.Contains($"≤ {typed}", TestHelpers.Text(cut.Markup)), Timeout);
        }

        [Fact]
        public async Task TogglingSwitch_UpdatesActiveCount()
        {
            await using var ctx = TestHelpers.CreateContext();
            var items = new List<Subscription>
            {
                new Subscription { Id = 1, Email = "a@example.com", CategoryName = "Дрова" }
            };
            ctx.AddState<Subscription>().SetItems(items);

            var cut = ctx.Render<SubscriptionsPage>();
            Assert.Contains(ctx.Localizer()["SubscriptionsTelegramActive", 1].Value, TestHelpers.Text(cut.Markup));

            cut.Find("input[type=checkbox]").Change(false);

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["SubscriptionsTelegramActive", 0].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.False(items[0].IsActive);
        }

        [Fact]
        public async Task FailedCreate_ShowsLocalizedError_AndDoesNotAddRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            var state = ctx.AddState<Subscription>();
            state.SetItems(new List<Subscription>());
            ctx.Services.GetRequiredService<FakeCatalogueApi>().FailWrites = true;

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("input")[0].Input("user@example.com");
            cut.FindAll("input")[1].Input("Дрова");
            await cut.Find("form").SubmitAsync();

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["ErrorSubscriptionExists"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.Empty(state.Items);
        }

        [Fact]
        public async Task FailedToggle_RevertsSwitch_AndShowsError()
        {
            await using var ctx = TestHelpers.CreateContext();
            var items = new List<Subscription> { new Subscription { Id = 1, Email = "a@example.com", CategoryName = "Дрова" } };
            ctx.AddState<Subscription>().SetItems(items);
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();
            api.FailWrites = true;

            var cut = ctx.Render<SubscriptionsPage>();
            cut.Find("input[type=checkbox]").Change(false);

            cut.WaitForAssertion(() =>
                Assert.Contains(ctx.Localizer()["ErrorApiUnavailable"].Value, TestHelpers.Text(cut.Markup)), Timeout);
            Assert.True(items[0].IsActive);
            Assert.Equal((1, false), Assert.Single(api.ActiveChanges));
        }

        [Fact]
        public async Task Delete_AfterConfirmation_CallsApi_AndRemovesRow()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);
            var state = ctx.AddState<Subscription>();
            state.SetItems(Sample());
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("button.mud-icon-button")[0].Click();

            cut.WaitForAssertion(() => Assert.Single(state.Items), Timeout);
            Assert.Equal(new[] { 1 }, api.DeletedIds);
            // Перевіряємо email рядка, а не назву категорії: у дефолтній (українській) культурі на Linux-runner
            // «Дрова» є і в статичному тексті сторінки (NextNotificationText), тож перевірка за назвою залежала від культури.
            Assert.DoesNotContain("a@example.com", TestHelpers.Text(cut.Markup));
        }

        [Fact]
        public async Task Delete_WithoutConfirmation_DoesNothing()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<bool>("confirm", _ => true).SetResult(false);
            var state = ctx.AddState<Subscription>();
            state.SetItems(Sample());
            var api = ctx.Services.GetRequiredService<FakeCatalogueApi>();

            var cut = ctx.Render<SubscriptionsPage>();
            cut.FindAll("button.mud-icon-button")[0].Click();

            cut.WaitForAssertion(() => Assert.Equal(2, state.Items.Count), Timeout);
            Assert.Empty(api.DeletedIds);
        }
    }
}