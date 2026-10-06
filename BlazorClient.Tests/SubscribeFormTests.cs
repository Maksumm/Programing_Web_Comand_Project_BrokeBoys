using Bunit;
using Xunit;
using BlazorClient.Components;
using MudBlazor.Services;

namespace BlazorClient.Tests
{
    public class SubscribeFormTests : BunitContext
    {
        public SubscribeFormTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddMudServices();
        }

        [Fact]
        public void Should_Show_ValidationErrors_When_SubmittingEmptyForm()
        {
            var cut = Render<SubscribeFormComponent>();

            // Знаходимо форму і відправляємо її
            var form = cut.Find("form");
            form.Submit();

            // Перевіряємо наявність повідомлень валідації
            Assert.Contains("Введіть електронну пошту", cut.Markup);
            Assert.Contains("Оберіть категорію", cut.Markup);
        }
    }
}