using ApiApp.Common;
using Microsoft.AspNetCore.SignalR;

namespace ApiApp.Realtime;

public interface ICatalogueNotifier
{
    /// <summary>Повідомляє групи вказаних категорій (і лише їх) про створення, зміну чи видалення товару.</summary>
    Task NotifyProductAsync(string kind, ProductRef product, IEnumerable<int> categoryIds, CancellationToken cancellationToken);

    /// <summary>Повідомляє групу категорії про зміну чи видалення самої категорії.</summary>
    Task NotifyCategoryAsync(string kind, int categoryId, string nameUk, string nameEn, CancellationToken cancellationToken);
}

/// <summary>Розсилає <see cref="CatalogueChanged"/> тією самою групою категорії, що й зміни цін. Збій розсилки не ламає запит: дані вже збережено.</summary>
public sealed class CatalogueNotifier(IHubContext<PricesHub> hub, ILogger<CatalogueNotifier> logger) : ICatalogueNotifier
{
    public Task NotifyProductAsync(string kind, ProductRef product, IEnumerable<int> categoryIds, CancellationToken cancellationToken) =>
        SendAsync("Product", kind, product.Id, product.NameUk, product.NameEn, categoryIds, cancellationToken);

    public Task NotifyCategoryAsync(string kind, int categoryId, string nameUk, string nameEn, CancellationToken cancellationToken) =>
        SendAsync("Category", kind, null, nameUk, nameEn, [categoryId], cancellationToken);

    private async Task SendAsync(string entity, string kind, int? productId, string nameUk, string nameEn, IEnumerable<int> categoryIds, CancellationToken cancellationToken)
    {
        var categories = categoryIds.Distinct().ToArray();
        if (categories.Length == 0)
        {
            return;
        }

        // Окремий span, як і для цін: у трасі запиту видно, скільки часу зайняла розсилка.
        using var activity = Telemetry.Source.StartActivity("BroadcastCatalogueChange");
        activity?.SetTag("catalogue.entity", entity);
        activity?.SetTag("catalogue.kind", kind);
        activity?.SetTag("category.ids", string.Join(",", categories));

        try
        {
            foreach (var categoryId in categories)
            {
                await hub.Clients.Groups(PricesHub.GroupName(categoryId))
                    .SendAsync("CatalogueChanged", new CatalogueChanged(entity, kind, categoryId, productId, nameUk, nameEn), cancellationToken);
            }

            Log.CatalogueBroadcast(logger, entity, kind, categories.Length);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            Log.CatalogueBroadcastFailed(logger, ex, entity);
        }
    }
}
