namespace ApiApp.Data;

// Незмінні записи: сховище міняє запис, створюючи новий (`with`), тому прочитані об'єкти безпечні для потоків.
public sealed record Category(int Id, string NameUk, string NameEn, string DescriptionUk, string DescriptionEn);

public sealed record Product(int Id, int CategoryId, string NameUk, string NameEn);

public sealed record SeasonalPrice(int Id, int ProductId, int Month, decimal Price);

public sealed record Subscription(int Id, string Email, int CategoryId, decimal? Threshold, bool IsActive);

/// <summary>Категорія разом з обчислюваними лічильниками (вони ніколи не зберігаються).</summary>
public sealed record CategoryView(Category Category, int ProductsCount, int SubscriptionsCount);
