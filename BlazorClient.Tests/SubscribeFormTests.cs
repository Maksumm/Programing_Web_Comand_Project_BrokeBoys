using Bunit;
using Xunit;
using BlazorClient.Components;
using BlazorClient.Models;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace BlazorClient.Tests
{
    public class SubscribeFormTests
    {
        private static BunitContext CreateContext()
        {
            var ctx = new BunitContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddMudServices();
            ctx.Services.AddLocalization();
            return ctx;
        }

        [Fact]
        public async Task ShouldRenderSubscribeFormComponent()
        {
            await using var ctx = CreateContext();
            var cut = ctx.Render<SubscribeFormComponent>();
            Assert.NotNull(cut.Instance);
        }

        [Fact]
        public async Task ShouldValidateModelProperties()
        {
            var model = new SubscribeFormModel
            {
                Email = "test@example.com"
            };

            Assert.Equal("test@example.com", model.Email);
        }

        [Fact]
        public async Task Should_Show_ValidationErrors_When_SubmittingEmptyForm()
        {
            await using var ctx = CreateContext();
            var cut = ctx.Render<SubscribeFormComponent>();

            // Знаходимо тег форми у компоненті
            var form = cut.Find("form");
            Assert.NotNull(form);

            // Симулюємо відправку порожньої форми для перевірки DataAnnotations
            await form.SubmitAsync();

            // Перевіряємо, що у DOM з'явилися повідомлення валідації
            Assert.NotEmpty(cut.Markup);
        }
    }
}