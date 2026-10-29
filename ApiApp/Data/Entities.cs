namespace ApiApp.Data;

// Сутності схеми (3НФ). Лічильники на кшталт «кількість підписок» не зберігаються — їх обчислює запит.

public sealed class Category
{
    public int Id { get; set; }
    public string NameUk { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string DescriptionUk { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;

    public List<Product> Products { get; set; } = [];
    public List<Subscription> Subscriptions { get; set; } = [];
}

public sealed class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string NameUk { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

    public Category Category { get; set; } = null!;
    public List<SeasonalPrice> Prices { get; set; } = [];
}

public sealed class SeasonalPrice
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    /// <summary>Номер місяця 1–12.</summary>
    public int Month { get; set; }

    public decimal Price { get; set; }

    public Product Product { get; set; } = null!;
}

public sealed class Subscription
{
    public int Id { get; set; }

    /// <summary>Хто володіє підпискою: ідентифікатор користувача (<c>oid</c> з токена Entra ID). Кожен бачить і змінює лише свої.</summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>Куди надсилати сповіщення; завжди в нижньому регістрі. Не є ключем: пошту можна змінити.</summary>
    public string Email { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public decimal? Threshold { get; set; }
    public bool IsActive { get; set; }

    public Category Category { get; set; } = null!;
}
