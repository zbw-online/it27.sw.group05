using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class TrendChartAxisScaleTests
    {
        private const int GridLineCount = 4;

        private static readonly int[] ExpectedSafeDefaultTicks = [0, 1, 2, 3, 4];
        private static readonly int[] AscendingCheckOrderMaxima = [0, 1, 2, 3, 4, 5, 9, 10, 17, 99, 125, 1000];
        private static readonly decimal[] AscendingCheckRevenueMaxima = [0m, 1m, 2m, 3m, 4m, 9m, 125m, 48_500.75m];

        [TestMethod]
        public void ForOrders_WithNoDataPoints_ShouldReturnSafePositiveScale()
        {
            (int max, IReadOnlyList<int> ticks) = TrendChartAxisScale.ForOrders(0, GridLineCount);

            Assert.IsTrue(max > 0);
            CollectionAssert.AreEqual(ExpectedSafeDefaultTicks, (System.Collections.ICollection)ticks);
        }

        [TestMethod]
        public void ForOrders_WithAllZeroValues_ShouldReturnSafePositiveScale()
        {
            (int max, IReadOnlyList<int> ticks) = TrendChartAxisScale.ForOrders(0, GridLineCount);

            Assert.AreEqual(4, max);
            Assert.AreEqual(0, ticks[0]);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        public void ForOrders_WithSmallMaximum_ShouldCoverDataWithinAxis(int dataMax)
        {
            (int max, IReadOnlyList<int> ticks) = TrendChartAxisScale.ForOrders(dataMax, GridLineCount);

            Assert.IsTrue(max >= dataMax, $"Axis max {max} must cover the data max {dataMax}.");
            Assert.IsTrue(max > 0);
            Assert.AreEqual(0, ticks[0]);
        }

        [TestMethod]
        public void ForOrders_WithMaximumOne_ShouldShowDistinctZeroOneAndTwoTicks()
        {
            (_, IReadOnlyList<int> ticks) = TrendChartAxisScale.ForOrders(1, GridLineCount);

            CollectionAssert.Contains((System.Collections.ICollection)ticks, 0);
            CollectionAssert.Contains((System.Collections.ICollection)ticks, 1);
            CollectionAssert.Contains((System.Collections.ICollection)ticks, 2);
        }

        [TestMethod]
        public void ForOrders_WithMaximumTwo_ShouldShowDistinctZeroOneAndTwoTicks()
        {
            (_, IReadOnlyList<int> ticks) = TrendChartAxisScale.ForOrders(2, GridLineCount);

            CollectionAssert.Contains((System.Collections.ICollection)ticks, 0);
            CollectionAssert.Contains((System.Collections.ICollection)ticks, 1);
            CollectionAssert.Contains((System.Collections.ICollection)ticks, 2);
        }

        [TestMethod]
        [DataRow(9)]
        [DataRow(125)]
        public void ForOrders_WithLargerMaximum_ShouldCoverDataWithAscendingDistinctTicks(int dataMax)
        {
            (int max, IReadOnlyList<int> ticks) = TrendChartAxisScale.ForOrders(dataMax, GridLineCount);

            Assert.IsTrue(max >= dataMax);
            AssertStrictlyAscending(ticks);
        }

        [TestMethod]
        public void ForOrders_TicksShouldAlwaysBeStrictlyAscendingAndHaveNoDuplicates()
        {
            foreach (int dataMax in AscendingCheckOrderMaxima)
            {
                (_, IReadOnlyList<int> ticks) = TrendChartAxisScale.ForOrders(dataMax, GridLineCount);
                AssertStrictlyAscending(ticks);
                Assert.AreEqual(GridLineCount + 1, ticks.Count);
            }
        }

        [TestMethod]
        public void ForOrders_WithIdenticalNonZeroValuesEveryMonth_ShouldProduceAPositiveAxisMax()
        {
            (int max, _) = TrendChartAxisScale.ForOrders(3, GridLineCount);

            Assert.IsTrue(max >= 3);
        }

        [TestMethod]
        public void ForRevenue_WithNoDataPoints_ShouldReturnSafePositiveScale()
        {
            (decimal max, IReadOnlyList<decimal> ticks) = TrendChartAxisScale.ForRevenue(0m, GridLineCount);

            Assert.IsTrue(max > 0m);
            Assert.AreEqual(0m, ticks[0]);
        }

        [TestMethod]
        public void ForRevenue_WithHighRevenueAndLowOrderCount_ShouldScaleIndependentlyOfOrders()
        {
            (decimal revenueMax, _) = TrendChartAxisScale.ForRevenue(48_500m, GridLineCount);
            (int orderMax, _) = TrendChartAxisScale.ForOrders(2, GridLineCount);

            Assert.IsTrue(revenueMax >= 48_500m);
            Assert.IsTrue(orderMax >= 2);
        }

        [TestMethod]
        public void ForRevenue_WithLowRevenueAndHighOrderCount_ShouldScaleIndependentlyOfOrders()
        {
            (decimal revenueMax, _) = TrendChartAxisScale.ForRevenue(15m, GridLineCount);
            (int orderMax, _) = TrendChartAxisScale.ForOrders(80, GridLineCount);

            Assert.IsTrue(revenueMax >= 15m);
            Assert.IsTrue(orderMax >= 80);
        }

        [TestMethod]
        public void ForRevenue_TicksShouldAlwaysBeStrictlyAscendingAndHaveNoDuplicates()
        {
            foreach (decimal dataMax in AscendingCheckRevenueMaxima)
            {
                (_, IReadOnlyList<decimal> ticks) = TrendChartAxisScale.ForRevenue(dataMax, GridLineCount);
                AssertStrictlyAscending(ticks);
                Assert.AreEqual(GridLineCount + 1, ticks.Count);
            }
        }

        private static void AssertStrictlyAscending(IReadOnlyList<int> values)
        {
            for (int i = 1; i < values.Count; i++)
            {
                Assert.IsTrue(values[i] > values[i - 1], $"Tick {values[i]} at index {i} is not strictly greater than the previous tick {values[i - 1]}.");
            }
        }

        private static void AssertStrictlyAscending(IReadOnlyList<decimal> values)
        {
            for (int i = 1; i < values.Count; i++)
            {
                Assert.IsTrue(values[i] > values[i - 1], $"Tick {values[i]} at index {i} is not strictly greater than the previous tick {values[i - 1]}.");
            }
        }
    }
}
