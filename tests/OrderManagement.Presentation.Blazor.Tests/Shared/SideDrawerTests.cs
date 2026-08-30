using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class SideDrawerTests : Bunit.TestContext
    {
        public SideDrawerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

        [TestMethod]
        public void Render_ShowsTitleAndChildContent()
        {
            IRenderedComponent<SideDrawer> cut = RenderComponent<SideDrawer>(parameters => parameters
                .Add(p => p.Title, "Neuer Kunde")
                .AddChildContent("<p>Formularinhalt</p>"));

            Assert.AreEqual("Neuer Kunde", cut.Find(".app-drawer-header h2").TextContent);
            Assert.AreEqual("Formularinhalt", cut.Find(".app-drawer-body p").TextContent);
        }

        [TestMethod]
        public void Render_WithFooterContent_ShowsFooter()
        {
            IRenderedComponent<SideDrawer> cut = RenderComponent<SideDrawer>(parameters => parameters
                .Add(p => p.Title, "Neuer Kunde")
                .Add(p => p.FooterContent, builder => builder.AddMarkupContent(0, "<button>Speichern</button>")));

            Assert.AreEqual(1, cut.FindAll(".app-drawer-footer").Count);
        }

        [TestMethod]
        public void Render_WithoutFooterContent_OmitsFooter()
        {
            IRenderedComponent<SideDrawer> cut = RenderComponent<SideDrawer>(parameters => parameters
                .Add(p => p.Title, "Neuer Kunde"));

            Assert.AreEqual(0, cut.FindAll(".app-drawer-footer").Count);
        }

        [TestMethod]
        public void Render_HasDialogRoleAndCloseButton()
        {
            IRenderedComponent<SideDrawer> cut = RenderComponent<SideDrawer>(parameters => parameters
                .Add(p => p.Title, "Neuer Kunde"));

            Assert.AreEqual(1, cut.FindAll(".app-drawer-close").Count);
            Assert.AreEqual("Schliessen", cut.Find(".app-drawer-close").GetAttribute("aria-label"));
        }
    }
}
