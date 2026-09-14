using Bunit;

using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class KpiCardTests : BunitContext
    {
        [TestMethod]
        public void Render_ShowsLabelAndValue()
        {
            IRenderedComponent<KpiCard> cut = Render<KpiCard>(parameters => parameters
                .Add(p => p.Label, "Aufträge total")
                .Add(p => p.Value, "174"));

            Assert.AreEqual("Aufträge total", cut.Find(".kpi-card-label").TextContent);
            Assert.AreEqual("174", cut.Find(".kpi-card-value").TextContent);
        }

        [TestMethod]
        public void Render_WithoutTrendText_OmitsTrendElement()
        {
            IRenderedComponent<KpiCard> cut = Render<KpiCard>(parameters => parameters
                .Add(p => p.Label, "Umsatz")
                .Add(p => p.Value, "CHF 0.00"));

            Assert.AreEqual(0, cut.FindAll(".kpi-card-trend").Count);
        }

        [TestMethod]
        public void Render_WithTrendText_ShowsTrend()
        {
            IRenderedComponent<KpiCard> cut = Render<KpiCard>(parameters => parameters
                .Add(p => p.Label, "Umsatz")
                .Add(p => p.Value, "CHF 1.24 Mio.")
                .Add(p => p.TrendText, "+12% vs. Vormonat")
                .Add(p => p.TrendTone, StatusTone.Success));

            Assert.AreEqual("+12% vs. Vormonat", cut.Find(".kpi-card-trend").TextContent);
        }

        [TestMethod]
        public void Render_WithoutIcon_OmitsIconElement()
        {
            IRenderedComponent<KpiCard> cut = Render<KpiCard>(parameters => parameters
                .Add(p => p.Label, "Umsatz")
                .Add(p => p.Value, "CHF 0.00"));

            Assert.AreEqual(0, cut.FindAll(".kpi-card-icon").Count);
        }

        [TestMethod]
        public void Render_WithIcon_ShowsIconElement()
        {
            IRenderedComponent<KpiCard> cut = Render<KpiCard>(parameters => parameters
                .Add(p => p.Label, "Umsatz")
                .Add(p => p.Value, "CHF 0.00")
                .Add(p => p.Icon, builder => builder.AddMarkupContent(0, "<svg data-testid=\"icon\"></svg>")));

            Assert.AreEqual(1, cut.FindAll(".kpi-card-icon").Count);
        }
    }
}
