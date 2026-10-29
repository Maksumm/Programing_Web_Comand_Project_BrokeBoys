using BlazorClient.Models;
using BlazorClient.Pages;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorClient.Tests
{
    public class CategoriesPageTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

        private static List<Category> Sample() => new()
        {
            new Category { Id = 1, Name = "Овочі", Description = "Свіжі овочі", ProductsCount = 4, SubscriptionsCount = 3 },
            new Category { Id = 2, Name = "Фрукти", Description = "Сезонні фрукти", ProductsCount = 2, SubscriptionsCount = 5 }
        };

        [Fact]
        public async Task LoadingState_ShowsSpinnerOnly()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Category>().SetLoading();

            var cut = ctx.Render<CategoriesPage>();

            Assert.Single(cut.FindAll(".mud-progress-circular"));
            Assert.Empty(cut.FindAll(".mud-card"));
            Assert.Empty(cut.FindAll(".error-state"));
        }

        [Fact]
        public async Task ErrorState_ShowsErrorMessageAndRetry()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Category>().SetError();

            var cut = ctx.Render<CategoriesPage>();

            var text = cut.Find(".error-state").TextContent;
            Assert.Contains(ctx.Localizer()["ErrorLoadingCategories"].Value, text);
            Assert.Contains(ctx.Localizer()["TryAgain"].Value, text);
            Assert.Empty(cut.FindAll(".mud-progress-circular"));
            Assert.Single(cut.FindAll("button"));
        }

        [Fact]
        public async Task ErrorState_ShowsTheRealReason_AndHttpStatus()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Category>().SetError("ErrorForbidden", 403);

            var cut = ctx.Render<CategoriesPage>();

            var text = TestHelpers.Text(cut.Find(".error-state").TextContent);
            Assert.Contains(ctx.Localizer()["ErrorForbidden"].Value, text);
            Assert.Contains("HTTP 403", text);
            Assert.DoesNotContain(ctx.Localizer()["CheckInternetAndTryAgain"].Value, text);
        }

        [Fact]
        public async Task EmptyState_ShowsEmptyMessage()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Category>().SetItems(new List<Category>());

            var cut = ctx.Render<CategoriesPage>();

            Assert.Contains(ctx.Localizer()["CategoriesYetEmpty"].Value, cut.Find(".empty-state").TextContent);
            Assert.Empty(cut.FindAll(".mud-card"));
        }

        [Fact]
        public async Task SuccessState_ShowsCardForEachCategory()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Category>().SetItems(Sample());

            var cut = ctx.Render<CategoriesPage>();

            Assert.Equal(2, cut.FindAll(".mud-card").Count);
            var text = TestHelpers.Text(cut.Markup);
            Assert.Contains("Овочі", text);
            Assert.Contains("Фрукти", text);
            Assert.Contains(ctx.Localizer()["ProductsLabel"].Value + ": 4", text);
            Assert.Empty(cut.FindAll(".error-state"));
        }

        [Fact]
        public async Task ChangingState_ReRendersPage()
        {
            await using var ctx = TestHelpers.CreateContext();
            var state = ctx.AddState<Category>();
            state.SetLoading();

            var cut = ctx.Render<CategoriesPage>();
            Assert.Single(cut.FindAll(".mud-progress-circular"));

            state.SetItems(Sample());

            cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-card").Count), Timeout);
            Assert.Empty(cut.FindAll(".mud-progress-circular"));

            state.SetError();

            cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".error-state")), Timeout);
            Assert.Empty(cut.FindAll(".mud-card"));
        }

        [Fact]
        public async Task RetryButton_ReloadsData()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Category>().SetError();
            ctx.Services.GetRequiredService<FakeCatalogueApi>().Categories.AddRange(Sample());

            var cut = ctx.Render<CategoriesPage>();
            cut.Find("button").Click();

            cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-card")), Timeout);
            Assert.Empty(cut.FindAll(".error-state"));
        }

        [Fact]
        public async Task SubscribeButton_NavigatesToSubscriptions()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.AddState<Category>().SetItems(Sample());

            var cut = ctx.Render<CategoriesPage>();
            cut.FindAll("button")[0].Click();

            var nav = ctx.Services.GetRequiredService<NavigationManager>();
            Assert.Contains("/subscriptions?category=", nav.Uri);
        }
    }
}
