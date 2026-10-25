using Microsoft.EntityFrameworkCore;

namespace ApiApp.Data;

/// <summary>
/// Демо-дані для розробки й тестів (в Development за налаштуванням <c>Seed:Demo</c>). Не міграція: явні id в HasData не
/// зсувають послідовності PostgreSQL. Заповнює порожню базу кількома збереженнями, тож id завжди 1, 2, 3… у порядку додавання.
/// </summary>
public static class DemoData
{
    // Ціни за 12 місяців (січень…грудень), грн. Жовтень для дубових дров = 1500.
    private static readonly decimal[][] MonthlyPrices =
    [
        [1700, 1700, 1650, 1500, 1350, 1250, 1200, 1250, 1400, 1500, 1650, 1750], // дуб
        [1600, 1600, 1550, 1400, 1250, 1150, 1100, 1150, 1300, 1400, 1550, 1650], // береза
        [3900, 3850, 3800, 3700, 3600, 3500, 3450, 3500, 3600, 3300, 3500, 3800], // зимові шини
        [3600, 3500, 3300, 3100, 3000, 3000, 3050, 3100, 3200, 3400, 3500, 3600], // літні шини
        [230, 230, 225, 215, 210, 205, 205, 210, 215, 220, 225, 230],            // цемент
        [9800, 9800, 9700, 9400, 9200, 9000, 9000, 9100, 9300, 9500, 9700, 9800]  // цегла
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = new[]
        {
            new Category { NameUk = "Дрова", NameEn = "Firewood", DescriptionUk = "Колоті та пиляні дрова на сезон", DescriptionEn = "Split and sawn firewood for the season" },
            new Category { NameUk = "Шини", NameEn = "Tires", DescriptionUk = "Літні та зимові шини", DescriptionEn = "Summer and winter tires" },
            new Category { NameUk = "Будівельні матеріали", NameEn = "Building materials", DescriptionUk = "Цемент, цегла та інше", DescriptionEn = "Cement, bricks and more" }
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(cancellationToken);

        var products = new[]
        {
            new Product { CategoryId = categories[0].Id, NameUk = "Колоті дрова, дуб", NameEn = "Split firewood, oak" },
            new Product { CategoryId = categories[0].Id, NameUk = "Колоті дрова, береза", NameEn = "Split firewood, birch" },
            new Product { CategoryId = categories[1].Id, NameUk = "Зимові шини 205/55 R16", NameEn = "Winter tires 205/55 R16" },
            new Product { CategoryId = categories[1].Id, NameUk = "Літні шини 205/55 R16", NameEn = "Summer tires 205/55 R16" },
            new Product { CategoryId = categories[2].Id, NameUk = "Цемент М500, 25 кг", NameEn = "Cement M500, 25 kg" },
            new Product { CategoryId = categories[2].Id, NameUk = "Цегла червона, 1000 шт.", NameEn = "Red brick, 1000 pcs" }
        };
        db.Products.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);

        db.SeasonalPrices.AddRange(products.SelectMany((product, index) =>
            Enumerable.Range(1, 12).Select(month => new SeasonalPrice
            {
                ProductId = product.Id,
                Month = month,
                Price = MonthlyPrices[index][month - 1]
            })));
        await db.SaveChangesAsync(cancellationToken);

        db.Subscriptions.AddRange(
            new Subscription { Email = "user@example.com", CategoryId = categories[0].Id, Threshold = 90m, IsActive = true },
            new Subscription { Email = "user@example.com", CategoryId = categories[1].Id, Threshold = 85.5m, IsActive = true });
        await db.SaveChangesAsync(cancellationToken);
    }
}
