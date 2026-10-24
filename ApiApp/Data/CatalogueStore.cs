namespace ApiApp.Data;

/// <summary>
/// Дані в пам'яті процесу (робота 3). Singleton; усі операції під одним lock, тому сховище потокобезпечне.
/// У роботі 4 його замінить DbContext із тими самими правилами унікальності й каскадів.
/// </summary>
public sealed partial class CatalogueStore
{
    private readonly object _gate = new();
    private readonly List<Category> _categories = [];
    private readonly List<Product> _products = [];
    private readonly List<SeasonalPrice> _prices = [];
    private readonly List<Subscription> _subscriptions = [];
    private int _categoryId;
    private int _productId;
    private int _priceId;
    private int _subscriptionId;

    private static bool Same(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    // ---------- Categories ----------

    public IReadOnlyList<CategoryView> ListCategories()
    {
        lock (_gate)
        {
            return _categories.Select(View).ToList();
        }
    }

    public CategoryView? GetCategory(int id)
    {
        lock (_gate)
        {
            var category = _categories.FirstOrDefault(c => c.Id == id);
            return category is null ? null : View(category);
        }
    }

    public StoreResult<CategoryView> AddCategory(string nameUk, string nameEn, string descriptionUk, string descriptionEn)
    {
        lock (_gate)
        {
            if (CategoryNameTaken(nameUk, nameEn, exceptId: null))
            {
                return StoreResult<CategoryView>.Conflict("A category with this name already exists.");
            }

            var category = new Category(++_categoryId, nameUk, nameEn, descriptionUk, descriptionEn);
            _categories.Add(category);
            return StoreResult<CategoryView>.Ok(View(category));
        }
    }

    public StoreResult<CategoryView> ReplaceCategory(int id, string nameUk, string nameEn, string descriptionUk, string descriptionEn)
    {
        lock (_gate)
        {
            var index = _categories.FindIndex(c => c.Id == id);
            if (index < 0)
            {
                return StoreResult<CategoryView>.NotFound($"Category {id} was not found.");
            }

            if (CategoryNameTaken(nameUk, nameEn, exceptId: id))
            {
                return StoreResult<CategoryView>.Conflict("Another category already has this name.");
            }

            _categories[index] = _categories[index] with
            {
                NameUk = nameUk,
                NameEn = nameEn,
                DescriptionUk = descriptionUk,
                DescriptionEn = descriptionEn
            };
            return StoreResult<CategoryView>.Ok(View(_categories[index]));
        }
    }

    /// <summary>Категорію з товарами видалити не можна (Restrict); підписки на неї видаляються разом з нею (Cascade).</summary>
    public StoreResult<bool> DeleteCategory(int id)
    {
        lock (_gate)
        {
            var category = _categories.FirstOrDefault(c => c.Id == id);
            if (category is null)
            {
                return StoreResult<bool>.NotFound($"Category {id} was not found.");
            }

            if (_products.Any(p => p.CategoryId == id))
            {
                return StoreResult<bool>.Conflict("The category still has products; delete or move them first.");
            }

            _subscriptions.RemoveAll(s => s.CategoryId == id);
            _categories.Remove(category);
            return StoreResult<bool>.Ok(true);
        }
    }

    // Викликати лише під lock.
    private CategoryView View(Category category) => new(
        category,
        _products.Count(p => p.CategoryId == category.Id),
        _subscriptions.Count(s => s.CategoryId == category.Id));

    private bool CategoryNameTaken(string nameUk, string nameEn, int? exceptId) =>
        _categories.Any(c => c.Id != exceptId && (Same(c.NameUk, nameUk) || Same(c.NameEn, nameEn)));

    // ---------- Products ----------

    public IReadOnlyList<Product> ListProducts(int? categoryId)
    {
        lock (_gate)
        {
            return _products.Where(p => categoryId is null || p.CategoryId == categoryId).ToList();
        }
    }

    public Product? GetProduct(int id)
    {
        lock (_gate)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }
    }

    public StoreResult<Product> AddProduct(int categoryId, string nameUk, string nameEn)
    {
        lock (_gate)
        {
            if (_categories.All(c => c.Id != categoryId))
            {
                return StoreResult<Product>.InvalidReference("CategoryId", "Category does not exist.");
            }

            if (ProductNameTaken(categoryId, nameUk, nameEn, exceptId: null))
            {
                return StoreResult<Product>.Conflict("This category already has a product with this name.");
            }

            var product = new Product(++_productId, categoryId, nameUk, nameEn);
            _products.Add(product);
            return StoreResult<Product>.Ok(product);
        }
    }

    public StoreResult<Product> ReplaceProduct(int id, int categoryId, string nameUk, string nameEn)
    {
        lock (_gate)
        {
            var index = _products.FindIndex(p => p.Id == id);
            if (index < 0)
            {
                return StoreResult<Product>.NotFound($"Product {id} was not found.");
            }

            if (_categories.All(c => c.Id != categoryId))
            {
                return StoreResult<Product>.InvalidReference("CategoryId", "Category does not exist.");
            }

            if (ProductNameTaken(categoryId, nameUk, nameEn, exceptId: id))
            {
                return StoreResult<Product>.Conflict("This category already has a product with this name.");
            }

            _products[index] = _products[index] with { CategoryId = categoryId, NameUk = nameUk, NameEn = nameEn };
            return StoreResult<Product>.Ok(_products[index]);
        }
    }

    /// <summary>Ціни видаляються разом із товаром (Cascade).</summary>
    public StoreResult<bool> DeleteProduct(int id)
    {
        lock (_gate)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product is null)
            {
                return StoreResult<bool>.NotFound($"Product {id} was not found.");
            }

            _prices.RemoveAll(p => p.ProductId == id);
            _products.Remove(product);
            return StoreResult<bool>.Ok(true);
        }
    }

    private bool ProductNameTaken(int categoryId, string nameUk, string nameEn, int? exceptId) =>
        _products.Any(p => p.Id != exceptId && p.CategoryId == categoryId && (Same(p.NameUk, nameUk) || Same(p.NameEn, nameEn)));

    // ---------- Seasonal prices ----------

    public IReadOnlyList<SeasonalPrice> ListPrices(int? productId)
    {
        lock (_gate)
        {
            return _prices.Where(p => productId is null || p.ProductId == productId).OrderBy(p => p.ProductId).ThenBy(p => p.Month).ToList();
        }
    }

    public SeasonalPrice? GetPrice(int id)
    {
        lock (_gate)
        {
            return _prices.FirstOrDefault(p => p.Id == id);
        }
    }

    public StoreResult<SeasonalPrice> AddPrice(int productId, int month, decimal price)
    {
        lock (_gate)
        {
            if (_products.All(p => p.Id != productId))
            {
                return StoreResult<SeasonalPrice>.InvalidReference("ProductId", "Product does not exist.");
            }

            if (_prices.Any(p => p.ProductId == productId && p.Month == month))
            {
                return StoreResult<SeasonalPrice>.Conflict("This product already has a price for this month.");
            }

            var item = new SeasonalPrice(++_priceId, productId, month, price);
            _prices.Add(item);
            return StoreResult<SeasonalPrice>.Ok(item);
        }
    }

    public StoreResult<SeasonalPrice> ReplacePrice(int id, int productId, int month, decimal price)
    {
        lock (_gate)
        {
            var index = _prices.FindIndex(p => p.Id == id);
            if (index < 0)
            {
                return StoreResult<SeasonalPrice>.NotFound($"Price {id} was not found.");
            }

            if (_products.All(p => p.Id != productId))
            {
                return StoreResult<SeasonalPrice>.InvalidReference("ProductId", "Product does not exist.");
            }

            if (_prices.Any(p => p.Id != id && p.ProductId == productId && p.Month == month))
            {
                return StoreResult<SeasonalPrice>.Conflict("This product already has a price for this month.");
            }

            _prices[index] = _prices[index] with { ProductId = productId, Month = month, Price = price };
            return StoreResult<SeasonalPrice>.Ok(_prices[index]);
        }
    }

    public StoreResult<bool> DeletePrice(int id)
    {
        lock (_gate)
        {
            var removed = _prices.RemoveAll(p => p.Id == id);
            return removed == 0 ? StoreResult<bool>.NotFound($"Price {id} was not found.") : StoreResult<bool>.Ok(true);
        }
    }

    // ---------- Subscriptions ----------

    public IReadOnlyList<Subscription> ListSubscriptions(string? email)
    {
        lock (_gate)
        {
            return _subscriptions.Where(s => email is null || Same(s.Email, email)).ToList();
        }
    }

    public Subscription? GetSubscription(int id)
    {
        lock (_gate)
        {
            return _subscriptions.FirstOrDefault(s => s.Id == id);
        }
    }

    public StoreResult<Subscription> AddSubscription(string email, int categoryId, decimal? threshold, bool isActive)
    {
        lock (_gate)
        {
            if (_categories.All(c => c.Id != categoryId))
            {
                return StoreResult<Subscription>.InvalidReference("CategoryId", "Category does not exist.");
            }

            if (_subscriptions.Any(s => Same(s.Email, email) && s.CategoryId == categoryId))
            {
                return StoreResult<Subscription>.Conflict("This e-mail is already subscribed to this category.");
            }

            var item = new Subscription(++_subscriptionId, email, categoryId, threshold, isActive);
            _subscriptions.Add(item);
            return StoreResult<Subscription>.Ok(item);
        }
    }

    public StoreResult<Subscription> ReplaceSubscription(int id, string email, int categoryId, decimal? threshold, bool isActive)
    {
        lock (_gate)
        {
            var index = _subscriptions.FindIndex(s => s.Id == id);
            if (index < 0)
            {
                return StoreResult<Subscription>.NotFound($"Subscription {id} was not found.");
            }

            if (_categories.All(c => c.Id != categoryId))
            {
                return StoreResult<Subscription>.InvalidReference("CategoryId", "Category does not exist.");
            }

            if (_subscriptions.Any(s => s.Id != id && Same(s.Email, email) && s.CategoryId == categoryId))
            {
                return StoreResult<Subscription>.Conflict("This e-mail is already subscribed to this category.");
            }

            _subscriptions[index] = _subscriptions[index] with
            {
                Email = email,
                CategoryId = categoryId,
                Threshold = threshold,
                IsActive = isActive
            };
            return StoreResult<Subscription>.Ok(_subscriptions[index]);
        }
    }

    public StoreResult<bool> DeleteSubscription(int id)
    {
        lock (_gate)
        {
            var removed = _subscriptions.RemoveAll(s => s.Id == id);
            return removed == 0 ? StoreResult<bool>.NotFound($"Subscription {id} was not found.") : StoreResult<bool>.Ok(true);
        }
    }
}
