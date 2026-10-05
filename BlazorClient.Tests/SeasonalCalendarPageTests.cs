using Bunit;
using Xunit;
using BlazorClient.Pages;
using BlazorClient.Models;
using MudBlazor.Services;
using System.Collections.Generic;
using System;

namespace BlazorClient.Tests
{
    public class SeasonalCalendarPageTests : BunitContext
    {
        public SeasonalCalendarPageTests()
        {
            Services.AddMudServices();
        }

        [Fact]
        public void Should_Display_LoadingState_When_FetchingPrices()
        {
            var cut = Render<SeasonalCalendarPage>(p => p.Add(c => c.IsLoading, true));

            Assert.Contains("mud-progress-circular", cut.Markup);
        }

        [Fact]
        public void Should_Display_EmptyState_When_NoPricesAvailable()
        {
            var cut = Render<SeasonalCalendarPage>(p => p.Add(c => c.IsLoading, false)
                                                         .Add(c => c.Prices, new List<SeasonalPrice>()));

            cut.Find(".empty-state").MarkupMatches(@"<div class=""empty-state"">Сезонні ціни ще не додані</div>");
        }

        [Fact]
        public void Should_Display_ErrorState_When_HasErrorIsTrue()
        {
            var cut = Render<SeasonalCalendarPage>(p => p.Add(c => c.HasError, true));

            cut.Find(".error-state").MarkupMatches(@"<div class=""error-state"">Помилка завантаження календаря цін</div>");
        }

        [Fact]
        public void Should_Display_PricesList_When_DataIsLoaded()
        {
            var pricesList = new List<SeasonalPrice>
            {
                new SeasonalPrice { Id = 1, Title = "Осінній тариф", Price = 1500, StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(30) }
            };

            var cut = Render<SeasonalCalendarPage>(p => p.Add(c => c.Prices, pricesList));

            Assert.Contains("Осінній тариф", cut.Markup);
            Assert.Contains("1500", cut.Markup);
        }
    }
}