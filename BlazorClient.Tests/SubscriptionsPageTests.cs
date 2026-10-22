using BlazorClient.Models;
using BlazorClient.Pages;
using Bunit;
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
            var inputs = cut.FindAll("input");
            inputs[0].Input("user@example.com");
            inputs[1].Input("Овочі");

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
            var inputs = cut.FindAll("input");
            inputs[0].Input("user@example.com");
            inputs[1].Input("Овочі");
            inputs[2].Input(typed);

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
    }
}
