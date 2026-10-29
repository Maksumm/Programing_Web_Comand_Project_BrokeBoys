namespace BlazorClient.Services
{
    public enum AdviceKind
    {
        /// <summary>Недостатньо цін, щоб щось радити.</summary>
        None,

        /// <summary>Ціна біля річного мінімуму (або дешевшого місяця найближчим часом не буде).</summary>
        Buy,

        /// <summary>Попереду є помітно дешевший місяць.</summary>
        Wait,

        /// <summary>Ціна біля річного піку.</summary>
        Avoid
    }

    /// <param name="Kind">Що радимо.</param>
    /// <param name="TextKey">Ключ .resx із поясненням.</param>
    /// <param name="Month">Для <see cref="AdviceKind.Wait"/>: найдешевший місяць попереду (1–12).</param>
    /// <param name="Percent">Для <see cref="AdviceKind.Wait"/> — на скільки відсотків ціна впаде; для Buy/Avoid — на скільки вона вища за річний мінімум.</param>
    public sealed record PriceAdvice(AdviceKind Kind, string TextKey, int? Month, int Percent);

    /// <summary>Порада за реальними цінами товару на 12 місяців; єдине місце цієї логіки для календаря й екрана підписок.</summary>
    public static class PriceAdvisor
    {
        private const decimal NearThreshold = 0.05m;

        public static PriceAdvice Advise(IReadOnlyDictionary<int, decimal> pricesByMonth, int currentMonth)
        {
            if (pricesByMonth.Count < 2 || !pricesByMonth.TryGetValue(currentMonth, out var current) || current <= 0)
            {
                return new PriceAdvice(AdviceKind.None, "VerdictNoData", null, 0);
            }

            var min = pricesByMonth.Values.Min();
            var max = pricesByMonth.Values.Max();
            var aboveMinimum = Percent(current, min);

            if (current <= min * (1 + NearThreshold))
            {
                return new PriceAdvice(AdviceKind.Buy, "VerdictBuyText", null, aboveMinimum);
            }

            if (current >= max * (1 - NearThreshold))
            {
                return new PriceAdvice(AdviceKind.Avoid, "VerdictAvoidText", null, aboveMinimum);
            }

            // the cheapest of the following eleven months, wrapping past December
            int? bestMonth = null;
            var bestPrice = current;
            for (var step = 1; step < 12; step++)
            {
                var month = (currentMonth - 1 + step) % 12 + 1;
                if (pricesByMonth.TryGetValue(month, out var price) && price < bestPrice)
                {
                    bestMonth = month;
                    bestPrice = price;
                }
            }

            if (bestMonth is int target && bestPrice <= current * (1 - NearThreshold))
            {
                var drop = (int)Math.Round((double)((current - bestPrice) / current) * 100);
                return new PriceAdvice(AdviceKind.Wait, "VerdictWaitText", target, drop);
            }

            return new PriceAdvice(AdviceKind.Buy, "VerdictStableText", null, 0);
        }

        private static int Percent(decimal value, decimal baseline) =>
            baseline <= 0 ? 0 : (int)Math.Round((double)((value - baseline) / baseline) * 100);
    }
}
