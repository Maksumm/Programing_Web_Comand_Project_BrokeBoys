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
            Assert.Equal(2, cut.FindAll("input").Count);
            Assert.NotNull(cut.Find("select"));
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

            // Повідомлення під кожним обов'язковим полем: Email (MudTextField) і Category (список).
            Assert.Single(cut.FindAll(".mud-input-helper-text.mud-input-error"));
            Assert.Single(cut.FindAll(".validation-message"));

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
                .Add(c => c.Categories, new List<Category> { new() { Id = 1, Name = "Овочі" } })
                .Add(c => c.OnSubmitted, EventCallback.Factory.Create<SubscribeFormModel>(
                    this, (SubscribeFormModel m) => { received = m; })));

            cut.FindAll("input")[0].Input("user@example.com");
            cut.Find("select").Change("Овочі");
            cut.FindAll("input")[1].Input(typed);

            await cut.Find("form").SubmitAsync();

            Assert.NotNull(received);
            Assert.Equal(3.5m, received!.Threshold);
            Assert.Equal("user@example.com", received.Email);
            Assert.Equal("Овочі", received.Category);
        }

        [Fact]
        public async Task CategoryDropdown_ListsTheGivenCategories_WithAPlaceholderFirst()
        {
            await using var ctx = CreateContext();

            var cut = ctx.Render<SubscribeFormComponent>(p => p
                .Add(c => c.Categories, new List<Category>
                {
                    new() { Id = 1, Name = "Дрова" },
                    new() { Id = 2, Name = "Шини" }
                }));

            var options = cut.FindAll("select option");
            Assert.Equal(3, options.Count);
            Assert.Equal(string.Empty, options[0].GetAttribute("value"));
            Assert.Equal("Дрова", options[1].GetAttribute("value"));
            Assert.Equal("Шини", options[2].GetAttribute("value"));
        }

        [Fact]
        public async Task InitialCategory_IsPreselected_WhenItExists()
        {
            await using var ctx = CreateContext();

            var cut = ctx.Render<SubscribeFormComponent>(p => p
                .Add(c => c.Categories, new List<Category> { new() { Id = 1, Name = "Дрова" }, new() { Id = 2, Name = "Шини" } })
                .Add(c => c.InitialCategory, "Шини"));

            Assert.Equal("Шини", cut.Find("select").SelectedValue());
        }

        [Fact]
        public async Task SubmittingWithoutChoosingACategory_DoesNotSubmit()
        {
            await using var ctx = CreateContext();
            var submitted = false;

            var cut = ctx.Render<SubscribeFormComponent>(p => p
                .Add(c => c.Categories, new List<Category> { new() { Id = 1, Name = "Дрова" } })
                .Add(c => c.OnSubmitted, EventCallback.Factory.Create<SubscribeFormModel>(
                    this, (SubscribeFormModel _) => { submitted = true; })));

            cut.FindAll("input")[0].Input("user@example.com");
            await cut.Find("form").SubmitAsync();

            Assert.False(submitted);
            Assert.Single(cut.FindAll(".validation-message"));
        }

        [Fact]
        public async Task InitialEmail_FillsTheEmailField()
        {
            await using var ctx = CreateContext();

            var cut = ctx.Render<SubscribeFormComponent>(p => p.Add(c => c.InitialEmail, "me@example.com"));

            Assert.Equal("me@example.com", cut.FindAll("input")[0].GetAttribute("value"));
        }

        [Fact]
        public async Task AfterASuccessfulSubmit_TheEmailGoesBackToTheInitialOne()
        {
            await using var ctx = CreateContext();
            SubscribeFormModel? received = null;

            var cut = ctx.Render<SubscribeFormComponent>(p => p
                .Add(c => c.Categories, new List<Category> { new() { Id = 1, Name = "Дрова" } })
                .Add(c => c.InitialEmail, "me@example.com")
                .Add(c => c.OnSubmitted, EventCallback.Factory.Create<SubscribeFormModel>(
                    this, (SubscribeFormModel m) => { received = m; })));

            cut.FindAll("input")[0].Input("work@example.com");
            cut.Find("select").Change("Дрова");
            await cut.Find("form").SubmitAsync();

            Assert.Equal("work@example.com", received?.Email);
            Assert.Equal("me@example.com", cut.FindAll("input")[0].GetAttribute("value"));
        }
    }
}
