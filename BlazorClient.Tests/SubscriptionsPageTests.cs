using Bunit;
using Xunit;
using BlazorClient.Pages;
using BlazorClient.Models;
using MudBlazor.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Threading;

namespace BlazorClient.Tests
{
    public class DummyHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("[]")
            });
        }
    }

    public class SubscriptionsPageTests
    {
        private static BunitContext CreateContext()
        {
            var ctx = new BunitContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddMudServices();
            ctx.Services.AddLocalization();

            var httpClient = new HttpClient(new DummyHttpMessageHandler())
            {
                BaseAddress = new Uri("http://localhost")
            };
            ctx.Services.AddSingleton(httpClient);

            return ctx;
        }

        [Fact]
        public async Task ShouldRenderLoadingState()
        {
            await using var ctx = CreateContext();

            var cut = ctx.Render<SubscriptionsPage>(p => p
                .Add(c => c.IsLoading, true));

            Assert.NotNull(cut.Instance);
        }

        [Fact]
        public async Task ShouldRenderErrorState()
        {
            await using var ctx = CreateContext();

            var cut = ctx.Render<SubscriptionsPage>(p => p
                .Add(c => c.HasError, true));

            Assert.NotNull(cut.Instance);
        }

        [Fact]
        public async Task ShouldRenderEmptyState()
        {
            await using var ctx = CreateContext();

            var cut = ctx.Render<SubscriptionsPage>(p => p
                .Add(c => c.Subscriptions, new List<Subscription>()));

            Assert.NotNull(cut.Instance);
        }

        [Fact]
        public async Task ShouldRenderSuccessStateWithData()
        {
            await using var ctx = CreateContext();

            var items = new List<Subscription>
            {
                new Subscription
                {
                    Id = 1,
                    Email = "user@example.com",
                    CategoryName = "Овочі"
                }
            };

            var cut = ctx.Render<SubscriptionsPage>(p => p
                .Add(c => c.Subscriptions, items));

            Assert.NotNull(cut.Instance);
        }
    }
}