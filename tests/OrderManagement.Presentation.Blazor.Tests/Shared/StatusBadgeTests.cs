using Bunit;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class StatusBadgeTests : Bunit.TestContext
    {
        [TestMethod]
        public void Render_WithSuccessTone_AppliesSuccessClassAndText()
        {
            IRenderedComponent<StatusBadge> cut = RenderComponent<StatusBadge>(parameters => parameters
                .Add(p => p.Text, "Aktiv")
                .Add(p => p.Tone, StatusTone.Success));

            Assert.IsTrue(cut.Find("span.status-badge").ClassList.Contains("status-badge-success"));
            Assert.IsTrue(cut.Markup.Contains("Aktiv"));
        }

        [TestMethod]
        public void Render_DoesNotRelyOnColorAlone_RendersVisibleTextLabel()
        {
            IRenderedComponent<StatusBadge> cut = RenderComponent<StatusBadge>(parameters => parameters
                .Add(p => p.Text, "Tiefer Bestand")
                .Add(p => p.Tone, StatusTone.Warning));

            Assert.AreEqual("Tiefer Bestand", cut.Find("span.status-badge").TextContent.Trim());
        }
    }
}
