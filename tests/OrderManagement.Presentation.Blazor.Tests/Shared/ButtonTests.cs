using Bunit;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class ButtonTests : Bunit.TestContext
    {
        [TestMethod]
        public void Render_WithPrimaryVariant_AppliesPrimaryClass()
        {
            IRenderedComponent<Button> cut = RenderComponent<Button>(parameters => parameters
                .Add(p => p.Variant, ButtonVariant.Primary)
                .AddChildContent("Speichern"));

            Assert.IsTrue(cut.Find("button").ClassList.Contains("app-button-primary"));
            Assert.AreEqual("Speichern", cut.Find("button").TextContent.Trim());
        }

        [TestMethod]
        public void Render_WhenDisabled_RendersDisabledAttribute()
        {
            IRenderedComponent<Button> cut = RenderComponent<Button>(parameters => parameters
                .Add(p => p.Disabled, true));

            Assert.IsTrue(cut.Find("button").HasAttribute("disabled"));
        }

        [TestMethod]
        public void Click_WhenEnabled_InvokesOnClickCallback()
        {
            bool clicked = false;
            IRenderedComponent<Button> cut = RenderComponent<Button>(parameters => parameters
                .Add(p => p.OnClick, () => clicked = true));

            cut.Find("button").Click();

            Assert.IsTrue(clicked);
        }

        [TestMethod]
        public void Render_WithoutExplicitVariant_DefaultsToSecondary()
        {
            IRenderedComponent<Button> cut = RenderComponent<Button>();

            Assert.IsTrue(cut.Find("button").ClassList.Contains("app-button-secondary"));
        }
    }
}
