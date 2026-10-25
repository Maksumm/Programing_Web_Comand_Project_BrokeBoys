using ApiApp.Common;
using Microsoft.AspNetCore.SignalR;

namespace ApiApp.Realtime;

public interface IPriceNotifier
{
    /// <summary>Повідомляє групи вказаних категорій (і лише їх) про зміну ціни. Помилка розсилки не ламає запит: дані вже збережено.</summary>
    Task NotifyAsync(string kind, int productId, int month, decimal? price, IEnumerable<int> categoryIds, CancellationToken cancellationToken);
}

public sealed class PriceNotifier(IHubContext<PricesHub> hub, ILogger<PriceNotifier> logger) : IPriceNotifier
{
    public async Task NotifyAsync(string kind, int productId, int month, decimal? price, IEnumerable<int> categoryIds, CancellationToken cancellationToken)
    {
        var categories = categoryIds.Distinct().ToArray();
        if (categories.Length == 0)
        {
            return;
        }

        // Розсилка — окремий span: у трасі запиту видно, скільки часу вона зайняла.
        using var activity = Telemetry.Source.StartActivity("BroadcastPriceChange");
        activity?.SetTag("price.kind", kind);
        activity?.SetTag("product.id", productId);
        activity?.SetTag("category.ids", string.Join(",", categories));

        try
        {
            foreach (var categoryId in categories)
            {
                await hub.Clients.Groups(PricesHub.GroupName(categoryId))
                    .SendAsync("PriceChanged", new PriceChanged(kind, categoryId, productId, month, price), cancellationToken);
            }

            Log.PriceBroadcast(logger, kind, productId, categories.Length);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            Log.BroadcastFailed(logger, ex, productId);
        }
    }
}
