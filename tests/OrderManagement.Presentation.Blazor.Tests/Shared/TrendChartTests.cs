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
    }
}
