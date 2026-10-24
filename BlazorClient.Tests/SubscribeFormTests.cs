using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BlazorClient.Components;
using BlazorClient.Models;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace BlazorClient.Tests
{
    public class SubscribeFormTests
    {
        private static BunitContext CreateContext() => TestHelpers.CreateContext();

        [Fact]
        public async Task ShouldRenderSubscribeFormComponent()
        {
            await using var ctx = CreateContext();

            var cut = ctx.Render<SubscribeFormComponent>();

            Assert.NotNull(cut.Find("form"));
            Assert.Equal(3, cut.FindAll("input").Count);
            Assert.NotNull(cut.Find("button[type=submit]"));
        }

        [Fact]
        public void EmptyModel_FailsDataAnnotationsValidation()
        {
            var model = new SubscribeFormModel();
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(
                model, new ValidationContext(model), results, validateAllProperties: true);

            Assert.False(isValid);

            var failedMembers = results.SelectMany(r => r.MemberNames).ToList();
            Assert.Contains(nameof(SubscribeFormModel.Email), failedMembers);
            Assert.Contains(nameof(SubscribeFormModel.Category), failedMembers);
            // Threshold необов'язковий: порожнє значення валідне.
            Assert.DoesNotContain(nameof(SubscribeFormModel.Threshold), failedMembers);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1001)]
        public void Threshold_OutOfRange_FailsValidation(double value)
        {
            var model = new SubscribeFormModel
            {
                Email = "user@example.com",
                Category = "Овочі",
                Threshold = (decimal)value
            };
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(
                model, new ValidationContext(model), results, validateAllProperties: true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(SubscribeFormModel.Threshold)));
        }

        [Fact]
        public async Task Should_Show_ValidationErrors_When_SubmittingEmptyForm()
        {
            await using var ctx = CreateContext();
            var submitted = false;

            var cut = ctx.Render<SubscribeFormComponent>(p => p
                .Add(c => c.OnSubmitted, EventCallback.Factory.Create<SubscribeFormModel>(
                    this, (SubscribeFormModel _) => { submitted = true; })));

            await cut.Find("form").SubmitAsync();

            // Повідомлення під кожним обов'язковим полем: Email і Category.
            var errors = cut.FindAll(".mud-input-helper-text.mud-input-error");
            Assert.Equal(2, errors.Count);

            // Некоректна форма не повинна викликати OnSubmitted.
            Assert.False(submitted);
        }

        [Theory]
        [InlineData("uk-UA", "3,5")]
        [InlineData("en-US", "3.5")]
        public async Task Threshold_ParsesFractionalNumber_ForCulture(string culture, string typed)
        {
            using var scope = new CultureScope(culture);
            await using var ctx = CreateContext();
            SubscribeFormModel? received = null;

            var cut = ctx.Render<SubscribeFormComponent>(p => p
                .Add(c => c.OnSubmitted, EventCallback.Factory.Create<SubscribeFormModel>(
                    this, (SubscribeFormModel m) => { received = m; })));

            cut.FindAll("input")[0].Input("user@example.com");
            cut.FindAll("input")[1].Input("Овочі");
            cut.FindAll("input")[2].Input(typed);

            await cut.Find("form").SubmitAsync();

            Assert.NotNull(received);
            Assert.Equal(3.5m, received!.Threshold);
            Assert.Equal("user@example.com", received.Email);
            Assert.Equal("Овочі", received.Category);
        }
    }
}
