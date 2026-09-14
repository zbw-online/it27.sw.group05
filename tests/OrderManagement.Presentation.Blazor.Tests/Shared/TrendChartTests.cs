using System.Globalization;

using AngleSharp.Dom;

using Bunit;

using OrderManagement.Application.Features.Orders.GetDashboardOverview;
using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class TrendChartTests : BunitContext
    {
        [TestMethod]
        public void Render_ShowsGridlines()
        {
            MonthlyTrendPointDto[] points =
            [
                new(2026, 1, 4, 120m),
                new(2026, 2, 9, 340m),
                new(2026, 3, 6, 210m)
            ];

            IRenderedComponent<TrendChart> cut = Render<TrendChart>(parameters => parameters
                .Add(p => p.Points, points));

            Assert.IsTrue(cut.FindAll(".trend-chart-gridline").Count > 0);
        }

        [TestMethod]
        public void Render_ShowsLeftAndRightAxisTicks()
        {
            MonthlyTrendPointDto[] points =
            [
                new(2026, 1, 4, 120m),
                new(2026, 2, 9, 340m)
            ];

            IRenderedComponent<TrendChart> cut = Render<TrendChart>(parameters => parameters
                .Add(p => p.Points, points));

            Assert.IsTrue(cut.FindAll(".trend-chart-axis-left span").Count > 0);
            Assert.IsTrue(cut.FindAll(".trend-chart-axis-right span").Count > 0);
        }

        [TestMethod]
        public void Render_ShowsMonthLabelForEachPoint()
        {
            MonthlyTrendPointDto[] points =
            [
                new(2026, 1, 4, 120m),
                new(2026, 2, 9, 340m),
                new(2026, 3, 6, 210m)
            ];

            IRenderedComponent<TrendChart> cut = Render<TrendChart>(parameters => parameters
                .Add(p => p.Points, points));

            Assert.AreEqual(3, cut.FindAll(".trend-chart-axis-bottom span").Count);
        }

        [TestMethod]
        public void Render_WithRealisticOrderDistribution_PositionsPointsCorrectlyOnOrdersAxis()
        {
            // Okt..Dez 2025, Jan..Sep 2026 - also exercises the turn of the year.
            MonthlyTrendPointDto[] points =
            [
                new(2025, 10, 1, 500m),
                new(2025, 11, 0, 0m),
                new(2025, 12, 0, 0m),
                new(2026, 1, 1, 480m),
                new(2026, 2, 0, 0m),
                new(2026, 3, 1, 510m),
                new(2026, 4, 1, 495m),
                new(2026, 5, 0, 0m),
                new(2026, 6, 1, 520m),
                new(2026, 7, 0, 0m),
                new(2026, 8, 1, 505m),
                new(2026, 9, 2, 990m),
            ];

            IRenderedComponent<TrendChart> cut = Render<TrendChart>(parameters => parameters
                .Add(p => p.Points, points));

            double[] cy = [.. ParseCy(cut.FindAll("svg circle"))];
            Assert.AreEqual(12, cy.Length);

            foreach (double y in cy)
            {
                Assert.IsTrue(double.IsFinite(y), "Every order point must have a finite cy coordinate.");
                Assert.IsTrue(y is >= 0 and <= 160, $"cy {y} must stay within the SVG viewBox height.");
            }

            int[] zeroOrderIndexes = [1, 2, 4, 7, 9]; // Nov, Dez, Feb, Mai, Jul
            int[] oneOrderIndexes = [0, 3, 5, 6, 8, 10]; // Okt, Jan, Mär, Apr, Jun, Aug
            const int septemberIndex = 11;

            double zeroY = cy[zeroOrderIndexes[0]];
            foreach (int index in zeroOrderIndexes)
            {
                Assert.AreEqual(zeroY, cy[index], 0.001, $"Zero-order month at index {index} must sit on the baseline.");
            }

            double oneY = cy[oneOrderIndexes[0]];
            foreach (int index in oneOrderIndexes)
            {
                Assert.AreEqual(oneY, cy[index], 0.001, $"Single-order month at index {index} must be at the same height as the others.");
            }

            Assert.IsTrue(cy[septemberIndex] < oneY, "September (2 Aufträge) muss sichtbar oberhalb der 1-Auftrag-Monate liegen.");
            Assert.IsTrue(oneY < zeroY, "1-Auftrag-Monate müssen sichtbar oberhalb der 0-Auftrag-Monate liegen.");
        }

        [TestMethod]
        public void Render_WithMaxOrderCountOfTwo_ShowsZeroOneAndTwoOnLeftAxis()
        {
            MonthlyTrendPointDto[] points =
            [
                new(2026, 1, 0, 0m),
                new(2026, 2, 1, 0m),
                new(2026, 3, 2, 0m),
            ];

            IRenderedComponent<TrendChart> cut = Render<TrendChart>(parameters => parameters
                .Add(p => p.Points, points));

            List<string> labels = [.. cut.FindAll(".trend-chart-axis-left span").Select(s => s.TextContent)];

            CollectionAssert.Contains(labels, "0");
            CollectionAssert.Contains(labels, "1");
            CollectionAssert.Contains(labels, "2");
        }

        [TestMethod]
        public void Render_WithSingleMonthHavingOneOrder_ProducesFiniteCoordinates()
        {
            // Regression test: this is exactly the input (max order count = 1) that previously
            // caused an integer-division-by-zero step and NaN/Infinity SVG coordinates.
            MonthlyTrendPointDto[] points = [new(2026, 9, 1, 100m)];

            IRenderedComponent<TrendChart> cut = Render<TrendChart>(parameters => parameters
                .Add(p => p.Points, points));

            IElement circle = cut.Find("svg circle");
            double cx = double.Parse(circle.GetAttribute("cx")!, CultureInfo.InvariantCulture);
            double cy = double.Parse(circle.GetAttribute("cy")!, CultureInfo.InvariantCulture);

            Assert.IsTrue(double.IsFinite(cx));
            Assert.IsTrue(double.IsFinite(cy));
        }

        [TestMethod]
        public void Render_WithAllZeroOrderCounts_ProducesFiniteBaselineCoordinates()
        {
            MonthlyTrendPointDto[] points =
            [
                new(2026, 1, 0, 0m),
                new(2026, 2, 0, 0m),
                new(2026, 3, 0, 0m),
            ];

            IRenderedComponent<TrendChart> cut = Render<TrendChart>(parameters => parameters
                .Add(p => p.Points, points));

            foreach (double y in ParseCy(cut.FindAll("svg circle")))
            {
                Assert.IsTrue(double.IsFinite(y));
            }
        }

        [TestMethod]
        public void Render_WithNoPoints_RendersWithoutCirclesOrInvalidCoordinates()
        {
            IRenderedComponent<TrendChart> cut = Render<TrendChart>(parameters => parameters
                .Add(p => p.Points, []));

            Assert.AreEqual(0, cut.FindAll("svg circle").Count);
        }

        [TestMethod]
        public void Render_WithIdenticalNonZeroValuesEveryMonth_ProducesFiniteCoordinatesAtTheSameHeight()
        {
            MonthlyTrendPointDto[] points =
            [
                new(2026, 1, 3, 300m),
                new(2026, 2, 3, 300m),
                new(2026, 3, 3, 300m),
            ];

            IRenderedComponent<TrendChart> cut = Render<TrendChart>(parameters => parameters
                .Add(p => p.Points, points));

            double[] cy = [.. ParseCy(cut.FindAll("svg circle"))];

            Assert.IsTrue(cy.All(double.IsFinite));
            Assert.IsTrue(cy.All(y => Math.Abs(y - cy[0]) < 0.001));
        }

        private static IEnumerable<double> ParseCy(IEnumerable<IElement> circles)
            => circles.Select(c => double.Parse(c.GetAttribute("cy")!, CultureInfo.InvariantCulture));
    }
}
