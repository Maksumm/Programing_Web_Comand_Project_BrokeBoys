using Microsoft.EntityFrameworkCore;

namespace ApiApp.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SeasonalPrice> SeasonalPrices => Set<SeasonalPrice>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.NameUk).HasMaxLength(200).IsRequired();
            entity.Property(c => c.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(c => c.DescriptionUk).HasMaxLength(1000).IsRequired();
            entity.Property(c => c.DescriptionEn).HasMaxLength(1000).IsRequired();

            // Кожне поле, що не може повторюватися, має власний унікальний індекс.
            entity.HasIndex(c => c.NameUk).IsUnique();
            entity.HasIndex(c => c.NameEn).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.NameUk).HasMaxLength(200).IsRequired();
            entity.Property(p => p.NameEn).HasMaxLength(200).IsRequired();

            // Назва товару унікальна в межах категорії (індекс також обслуговує зовнішній ключ).
            entity.HasIndex(p => new { p.CategoryId, p.NameUk }).IsUnique();
            entity.HasIndex(p => new { p.CategoryId, p.NameEn }).IsUnique();

            // Restrict: категорію з товарами видалити не можна — товари не мають зникати разом із нею.
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SeasonalPrice>(entity =>
        {
            entity.Property(p => p.Price).HasPrecision(12, 2);

            // Один товар — одна ціна на місяць.
            entity.HasIndex(p => new { p.ProductId, p.Month }).IsUnique();

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_SeasonalPrices_Month", "\"Month\" BETWEEN 1 AND 12");
                table.HasCheckConstraint("CK_SeasonalPrices_Price", "\"Price\" > 0");
            });

            // Cascade: ціни не існують без товару, тож видаляються разом із ним.
            entity.HasOne(p => p.Product)
                .WithMany(p => p.Prices)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.Property(s => s.OwnerId).HasMaxLength(128).IsRequired();
            entity.Property(s => s.Email).HasMaxLength(254).IsRequired();
            entity.Property(s => s.Threshold).HasPrecision(7, 2);

            // Один користувач — одна підписка на категорію.
            entity.HasIndex(s => new { s.OwnerId, s.CategoryId }).IsUnique();

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_Subscriptions_Threshold", "\"Threshold\" IS NULL OR \"Threshold\" BETWEEN 0.01 AND 1000"));

            // Cascade: підписка на видалену категорію втрачає сенс.
            entity.HasOne(s => s.Category)
                .WithMany(c => c.Subscriptions)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
