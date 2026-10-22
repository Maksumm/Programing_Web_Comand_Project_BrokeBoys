using System.ComponentModel.DataAnnotations;
using BlazorClient.Models;
using Xunit;

namespace BlazorClient.Tests
{
    public class LocalizationTests
    {
        [Theory]
        [InlineData("MySubscriptions", "Мої підписки", "My subscriptions")]
        [InlineData("Save", "Зберегти", "Save")]
        [InlineData("FilterAll", "Усі", "All")]
        public async Task Localizer_ReturnsTextForSelectedCulture(string key, string uk, string en)
        {
            await using var ctx = TestHelpers.CreateContext();

            using (new CultureScope("uk-UA"))
            {
                Assert.Equal(uk, ctx.Localizer()[key].Value);
            }

            using (new CultureScope("en-US"))
            {
                Assert.Equal(en, ctx.Localizer()[key].Value);
            }
        }

        [Fact]
        public async Task Localizer_FormatsNumbersByCulture()
        {
            await using var ctx = TestHelpers.CreateContext();

            using (new CultureScope("uk-UA"))
            {
                Assert.Contains("3,5", ctx.Localizer()["ThresholdLine", "Київ", 3.5m].Value);
            }

            using (new CultureScope("en-US"))
            {
                Assert.Contains("3.5", ctx.Localizer()["ThresholdLine", "Kyiv", 3.5m].Value);
            }
        }

        [Fact]
        public void ValidationMessages_FollowSelectedCulture()
        {
            static string EmailMessage(string culture)
            {
                using var scope = new CultureScope(culture);
                var model = new SubscribeFormModel();
                var results = new List<ValidationResult>();
                Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
                return results.First(r => r.MemberNames.Contains(nameof(SubscribeFormModel.Email))).ErrorMessage!;
            }

            Assert.Equal("Вкажіть електронну пошту", EmailMessage("uk-UA"));
            Assert.Equal("Enter your email", EmailMessage("en-US"));
        }
    }
}
