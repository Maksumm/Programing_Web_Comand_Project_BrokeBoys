using Bunit;
using Xunit;
using BlazorClient.Pages;
using BlazorClient.Models;
using MudBlazor.Services;
using System.Collections.Generic;

namespace BlazorClient.Tests
{
    
        public class SubscriptionsPageTests : BunitContext, IAsyncLifetime
        {
        public SubscriptionsPageTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddMudServices();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

        [Fact]
        public void Should_Display_LoadingState_When_FetchingSubscriptions()
        {
            var cut = Render<SubscriptionsPage>(p => p.Add(c => c.IsLoading, true));

            Assert.Contains("mud-progress-circular", cut.Markup);
        }

        [Fact]
        public void Should_Display_EmptyState_When_NoSubscriptionsAvailable()
        {
            var cut = Render<SubscriptionsPage>(p => p.Add(c => c.IsLoading, false)
                                                          .Add(c => c.Subscriptions, new List<Subscription>()));

            Assert.Contains("Абонементи відсутні", cut.Markup);
        }

        [Fact]
        public void Should_Display_ErrorState_When_HasErrorIsTrue()
        {
            var cut = Render<SubscriptionsPage>(p => p.Add(c => c.HasError, true));

            Assert.Contains("Помилка завантаження абонементів", cut.Markup);
        }

        [Fact]
        public void Should_Display_SubscriptionsList_When_DataIsLoaded()
        {
            var subscriptionsList = new List<Subscription>
            {
                new Subscription
                {
                    Id = 1,
                    Title = "Сезонний стандарт",
                    CategoryName = "Абонементи",
                    Price = 1500,
                    Duration = "1 місяць",
                    IsActive = true
                }
            };

            var cut = Render<SubscriptionsPage>(p => p.Add(c => c.Subscriptions, subscriptionsList));

            Assert.Contains("Сезонний стандарт", cut.Markup);
            Assert.Contains("Абонементи", cut.Markup);
            Assert.Contains("1 місяць", cut.Markup);
        }
    }
}