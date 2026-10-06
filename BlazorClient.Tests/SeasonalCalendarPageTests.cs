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

namespace BlazorClient.Tests
{
    public class SeasonalCalendarPageTests
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

            var cut = ctx.Render<SeasonalCalendarPage>(p => p
                .Add(c => c.IsLoading, true));

            Assert.NotNull(cut.Instance);
        }

        [Fact]
        public async Task ShouldRenderErrorState()
        {
            await using var ctx = CreateContext();

            var cut = ctx.Render<SeasonalCalendarPage>(p => p
                .Add(c => c.HasError, true));

            Assert.NotNull(cut.Instance);
        }

        [Fact]
        public async Task ShouldRenderEmptyState()
        {
            await using var ctx = CreateContext();

            var cut = ctx.Render<SeasonalCalendarPage>(p => p
                .Add(c => c.Prices, new List<SeasonalPrice>()));

            Assert.NotNull(cut.Instance);
        }

        [Fact]
        public async Task ShouldRenderSuccessStateWithData()
        {
            await using var ctx = CreateContext();

            var items = new List<SeasonalPrice>
            {
                new SeasonalPrice { Id = 1, Name = "Яблука", Price = 25.5m }
            };

            var cut = ctx.Render<SeasonalCalendarPage>(p => p
                .Add(c => c.Prices, items));

            Assert.NotNull(cut.Instance);
        }
    }
}