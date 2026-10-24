namespace ApiApp.Data;

public sealed partial class CatalogueStore
{
    // Ціни за 12 місяців (січень…грудень), грн. Жовтень для дубових дров = 1500.
    private static readonly decimal[] OakFirewood = [1700, 1700, 1650, 1500, 1350, 1250, 1200, 1250, 1400, 1500, 1650, 1750];
    private static readonly decimal[] BirchFirewood = [1600, 1600, 1550, 1400, 1250, 1150, 1100, 1150, 1300, 1400, 1550, 1650];
    private static readonly decimal[] WinterTires = [3900, 3850, 3800, 3700, 3600, 3500, 3450, 3500, 3600, 3300, 3500, 3800];
    private static readonly decimal[] SummerTires = [3600, 3500, 3300, 3100, 3000, 3000, 3050, 3100, 3200, 3400, 3500, 3600];
    private static readonly decimal[] Cement = [230, 230, 225, 215, 210, 205, 205, 210, 215, 220, 225, 230];
    private static readonly decimal[] RedBrick = [9800, 9800, 9700, 9400, 9200, 9000, 9000, 9100, 9300, 9500, 9700, 9800];

    /// <summary>Демо-дані, ті самі категорії, що були в клієнтському SampleData, але з цінами на 12 місяців.</summary>
    public static CatalogueStore CreateSeeded()
    {
        var store = new CatalogueStore();

        var firewood = store.AddCategory("Дрова", "Firewood", "Колоті та пиляні дрова на сезон", "Split and sawn firewood for the season").Value!.Category;
        var tires = store.AddCategory("Шини", "Tires", "Літні та зимові шини", "Summer and winter tires").Value!.Category;
        var materials = store.AddCategory("Будівельні матеріали", "Building materials", "Цемент, цегла та інше", "Cement, bricks and more").Value!.Category;

        store.AddPrices(store.AddProduct(firewood.Id, "Колоті дрова, дуб", "Split firewood, oak").Value!, OakFirewood);
        store.AddPrices(store.AddProduct(firewood.Id, "Колоті дрова, береза", "Split firewood, birch").Value!, BirchFirewood);
        store.AddPrices(store.AddProduct(tires.Id, "Зимові шини 205/55 R16", "Winter tires 205/55 R16").Value!, WinterTires);
        store.AddPrices(store.AddProduct(tires.Id, "Літні шини 205/55 R16", "Summer tires 205/55 R16").Value!, SummerTires);
        store.AddPrices(store.AddProduct(materials.Id, "Цемент М500, 25 кг", "Cement M500, 25 kg").Value!, Cement);
        store.AddPrices(store.AddProduct(materials.Id, "Цегла червона, 1000 шт.", "Red brick, 1000 pcs").Value!, RedBrick);

        store.AddSubscription("user@example.com", firewood.Id, 90m, true);
        store.AddSubscription("user@example.com", tires.Id, 85.5m, true);

        return store;
    }

    private void AddPrices(Product product, decimal[] monthlyPrices)
    {
        for (var month = 1; month <= 12; month++)
        {
            AddPrice(product.Id, month, monthlyPrices[month - 1]);
        }
    }
}
