using System.Globalization;
using BlazorClient.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace BlazorClient.Tests
{
    public class CultureServiceTests
    {
        private static CultureService Create(BunitContext ctx) =>
            new(ctx.Services.GetRequiredService<IJSRuntime>());

        [Fact]
        public async Task GetSavedCulture_ReturnsStoredCulture()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<string>("localStorage.getItem", CultureService.StorageKey).SetResult("en-US");

            Assert.Equal("en-US", await Create(ctx).GetSavedCultureAsync());
        }

        [Fact]
        public async Task GetSavedCulture_ReturnsDefault_WhenNothingStored()
        {
            await using var ctx = TestHelpers.CreateContext();

            Assert.Equal(CultureService.DefaultCulture, await Create(ctx).GetSavedCultureAsync());
        }

        [Fact]
        public async Task GetSavedCulture_ReturnsDefault_WhenStoredValueIsNotSupported()
        {
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<string>("localStorage.getItem", CultureService.StorageKey).SetResult("de-DE");

            Assert.Equal(CultureService.DefaultCulture, await Create(ctx).GetSavedCultureAsync());
        }

        [Fact]
        public async Task InitializeAsync_AppliesSavedCulture()
        {
            using var scope = new CultureScope("uk-UA");
            await using var ctx = TestHelpers.CreateContext();
            ctx.JSInterop.Setup<string>("localStorage.getItem", CultureService.StorageKey).SetResult("en-US");

            await Create(ctx).InitializeAsync();

            // Перевіряємо DefaultThread*: саме їх читає Blazor WASM після RunAsync(), а CurrentCulture,
            // встановлена після await усередині async-методу, не повертається викликачеві (ExecutionContext).
            Assert.Equal("en-US", CultureInfo.DefaultThreadCurrentUICulture?.Name);
            Assert.Equal("en-US", CultureInfo.DefaultThreadCurrentCulture?.Name);
        }

        [Fact]
        public async Task ChangeCulture_SavesChoiceAndReloadsPage()
        {
            await using var ctx = TestHelpers.CreateContext();

            await Create(ctx).ChangeCultureAsync("en-US");

            var saved = ctx.JSInterop.VerifyInvoke("localStorage.setItem");
            Assert.Equal(CultureService.StorageKey, saved.Arguments[0]);
            Assert.Equal("en-US", saved.Arguments[1]);
            ctx.JSInterop.VerifyInvoke("location.reload");
        }

        [Fact]
        public async Task ChangeCulture_RejectsUnsupportedCulture()
        {
            await using var ctx = TestHelpers.CreateContext();

            await Assert.ThrowsAsync<ArgumentException>(() => Create(ctx).ChangeCultureAsync("de-DE"));
            ctx.JSInterop.VerifyNotInvoke("localStorage.setItem");
        }
    }
}
