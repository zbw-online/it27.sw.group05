namespace OrderManagement.Presentation.Blazor.Components.Shared
{
    /// <summary>
    /// Computes a chart axis' upper bound and evenly spaced gridline ticks from the data's maximum
    /// value, always landing on a "nice" 1/2/5 step so the axis maximum is strictly positive - even
    /// for empty or all-zero data - and division by a zero axis range can never occur.
    /// </summary>
    public static class TrendChartAxisScale
    {
        private static readonly int[] NiceFactors = [1, 2, 5];

        public static (int Max, IReadOnlyList<int> Ticks) ForOrders(int dataMax, int gridLineCount)
        {
            int step = NiceIntStep(dataMax, gridLineCount);
            return (step * gridLineCount, [.. Enumerable.Range(0, gridLineCount + 1).Select(i => i * step)]);
        }

        public static (decimal Max, IReadOnlyList<decimal> Ticks) ForRevenue(decimal dataMax, int gridLineCount)
        {
            decimal step = NiceDecimalStep(dataMax, gridLineCount);
            return (step * gridLineCount, [.. Enumerable.Range(0, gridLineCount + 1).Select(i => i * step)]);
        }

        private static int NiceIntStep(int dataMax, int gridLineCount)
        {
            if (dataMax <= 0)
            {
                return 1;
            }

            for (int magnitude = 1; ; magnitude *= 10)
            {
                foreach (int factor in NiceFactors)
                {
                    int step = factor * magnitude;
                    if ((long)step * gridLineCount >= dataMax)
                    {
                        return step;
                    }
                }
            }
        }

        private static decimal NiceDecimalStep(decimal dataMax, int gridLineCount)
        {
            if (dataMax <= 0m)
            {
                return 1m;
            }

            for (decimal magnitude = 1m; ; magnitude *= 10m)
            {
                foreach (int factor in NiceFactors)
                {
                    decimal step = factor * magnitude;
                    if (step * gridLineCount >= dataMax)
                    {
                        return step;
                    }
                }
            }
        }
    }
}
