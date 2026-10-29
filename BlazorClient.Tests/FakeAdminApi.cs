using System.Globalization;
using System.Net;
using BlazorClient.Models;
using BlazorClient.Services;

namespace BlazorClient.Tests
{
    /// <summary>Підміна API адміністратора: тримає дані в пам'яті, як сервер, і запам'ятовує виклики запису.</summary>
    internal sealed class FakeAdminApi : IAdminApi
    {
        private int _nextId = 1000;

        public List<AdminCategory> Categories { get; } = new();

        public List<AdminProduct> Products { get; } = new();

        public List<AdminPrice> Prices { get; } = new();

        /// <summary>Усі записи у вигляді рядків («CreateCategory:назва…»), щоб тест міг перевірити, що саме надіслано.</summary>
        public List<string> Calls { get; } = new();

        /// <summary>Якщо задано, кожен запис повертає цю помилку (ключ .resx) і нічого не змінює.</summary>
        public string? WriteError { get; set; }

        /// <summary>Якщо true, читання кидають виняток 500.</summary>
        public bool FailReads { get; set; }

        private void CheckRead()
        {
            if (FailReads)
            {
                throw new HttpRequestException("down", null, HttpStatusCode.InternalServerError);
            }
        }

        private Task<ApiResult> Write(string call, Action apply)
        {
            Calls.Add(call);
            if (WriteError is not null)
            {
                return Task.FromResult(ApiResult.Fail(WriteError));
            }

            apply();
            return Task.FromResult(ApiResult.Ok());
        }

        private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);

        public Task<IReadOnlyList<AdminCategory>> GetCategoriesAsync()
        {
            CheckRead();
            return Task.FromResult<IReadOnlyList<AdminCategory>>(Categories.ToList());
        }

        public Task<IReadOnlyList<AdminProduct>> GetProductsAsync()
        {
            CheckRead();
            return Task.FromResult<IReadOnlyList<AdminProduct>>(Products.ToList());
        }

        public Task<IReadOnlyList<AdminPrice>> GetPricesAsync(int productId)
        {
            CheckRead();
            return Task.FromResult<IReadOnlyList<AdminPrice>>(Prices.Where(p => p.ProductId == productId).ToList());
        }

        public Task<ApiResult> CreateCategoryAsync(CategoryInput input) =>
            Write($"CreateCategory:{input.NameUk}|{input.NameEn}|{input.DescriptionUk}|{input.DescriptionEn}",
                () => Categories.Add(new AdminCategory(++_nextId, input.NameUk, input.NameEn, input.DescriptionUk, input.DescriptionEn, 0, 0)));

        public Task<ApiResult> ReplaceCategoryAsync(int id, CategoryInput input) =>
            Write($"ReplaceCategory:{id}:{input.NameUk}|{input.NameEn}|{input.DescriptionUk}|{input.DescriptionEn}", () =>
            {
                var index = Categories.FindIndex(c => c.Id == id);
                Categories[index] = Categories[index] with
                {
                    NameUk = input.NameUk,
                    NameEn = input.NameEn,
                    DescriptionUk = input.DescriptionUk,
                    DescriptionEn = input.DescriptionEn
                };
            });

        public Task<ApiResult> DeleteCategoryAsync(int id) =>
            Write($"DeleteCategory:{id}", () => Categories.RemoveAll(c => c.Id == id));

        public Task<ApiResult> CreateProductAsync(ProductInput input) =>
            Write($"CreateProduct:{input.CategoryId}|{input.NameUk}|{input.NameEn}",
                () => Products.Add(new AdminProduct(++_nextId, input.CategoryId, input.NameUk, input.NameEn)));

        public Task<ApiResult> ReplaceProductAsync(int id, ProductInput input) =>
            Write($"ReplaceProduct:{id}:{input.CategoryId}|{input.NameUk}|{input.NameEn}", () =>
            {
                var index = Products.FindIndex(p => p.Id == id);
                Products[index] = Products[index] with { CategoryId = input.CategoryId, NameUk = input.NameUk, NameEn = input.NameEn };
            });

        public Task<ApiResult> DeleteProductAsync(int id) =>
            Write($"DeleteProduct:{id}", () => Products.RemoveAll(p => p.Id == id));

        public Task<ApiResult> CreatePriceAsync(PriceInput input) =>
            Write($"CreatePrice:{input.ProductId}|{input.Month}|{Number(input.Price)}",
                () => Prices.Add(new AdminPrice(++_nextId, input.ProductId, input.Month, input.Price)));

        public Task<ApiResult> ReplacePriceAsync(int id, PriceInput input) =>
            Write($"ReplacePrice:{id}:{input.ProductId}|{input.Month}|{Number(input.Price)}", () =>
            {
                var index = Prices.FindIndex(p => p.Id == id);
                Prices[index] = Prices[index] with { ProductId = input.ProductId, Month = input.Month, Price = input.Price };
            });

        public Task<ApiResult> DeletePriceAsync(int id) =>
            Write($"DeletePrice:{id}", () => Prices.RemoveAll(p => p.Id == id));
    }
}
