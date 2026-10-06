using Bunit;
using Xunit;
using BlazorClient.Pages;
using BlazorClient.Models;
using MudBlazor.Services;
using System.Collections.Generic;

namespace BlazorClient.Tests
{
    public class CategoriesPageTests : BunitContext
    {
        public CategoriesPageTests()
        {
            Services.AddMudServices();
        }

        [Fact]
        public void Should_Display_LoadingState_When_FetchingCategories()
        {
            var cut = Render<CategoriesPage>(p => p.Add(c => c.IsLoading, true));

            Assert.Contains("mud-progress-circular", cut.Markup);
        }

        [Fact]
        public void Should_Display_EmptyState_When_NoCategoriesAvailable()
        {
            var cut = Render<CategoriesPage>(p => p.Add(c => c.IsLoading, false)
                                                         .Add(c => c.Categories, new List<Category>()));

            Assert.Contains("Категорії ще не додані", cut.Markup);
        }

        [Fact]
        public void Should_Display_ErrorState_When_HasErrorIsTrue()
        {
            var cut = Render<CategoriesPage>(p => p.Add(c => c.HasError, true));

            Assert.Contains("Помилка завантаження категорій", cut.Markup);
        }

        [Fact]
        public void Should_Display_CategoriesList_When_DataIsLoaded()
        {
            var categoriesList = new List<Category>
            {
                new Category
                {
                    Id = 1,
                    Name = "Ваша Назва Категорії",
                    Description = "Ваш опис категорії",
                    SubscriptionsCount = 5
                }
            };  
        }
    }
}