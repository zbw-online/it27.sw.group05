using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class ButtonTests : BunitContext
    {
        [TestMethod]
        public void Render_WithPrimaryVariant_AppliesPrimaryClass()
        {
            IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
                .Add(p => p.Variant, ButtonVariant.Primary)
                .AddChildContent("Speichern"));

            Assert.IsTrue(cut.Find("button").ClassList.Contains("app-button-primary"));
            Assert.AreEqual("Speichern", cut.Find("button").TextContent.Trim());
        }

        [TestMethod]
        public void Render_WhenDisabled_RendersDisabledAttribute()
        {
            IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
                .Add(p => p.Disabled, true));

            Assert.IsTrue(cut.Find("button").HasAttribute("disabled"));
        }

        [TestMethod]
        public void Click_WhenEnabled_InvokesOnClickCallback()
        {
            bool clicked = false;
            IRenderedComponent<Button> cut = Render<Button>(parameters => parameters
                .Add(p => p.OnClick, () => clicked = true));

            cut.Find("button").Click();

            Assert.IsTrue(clicked);
        }

        [TestMethod]
        public void Render_WithoutExplicitVariant_DefaultsToSecondary()
        {
            IRenderedComponent<Button> cut = Render<Button>();

            Assert.IsTrue(cut.Find("button").ClassList.Contains("app-button-secondary"));
        }

        [TestMethod]
        public void Click_WithStopPropagationTrue_DoesNotInvokeAncestorClickHandler()
        {
            bool ancestorClicked = false;
            bool buttonClicked = false;

            IRenderedComponent<IComponent> cut = Render(RenderButtonInsideClickableAncestor(
                stopPropagation: true,
                onAncestorClick: () => ancestorClicked = true,
                onButtonClick: () => buttonClicked = true));

            cut.Find("button").Click();

            Assert.IsTrue(buttonClicked);
            Assert.IsFalse(ancestorClicked, "StopPropagation=true must prevent the ancestor's click handler from firing.");
        }

        [TestMethod]
        public void Click_WithStopPropagationFalse_StillInvokesAncestorClickHandler()
        {
            bool ancestorClicked = false;
            bool buttonClicked = false;

            IRenderedComponent<IComponent> cut = Render(RenderButtonInsideClickableAncestor(
                stopPropagation: false,
                onAncestorClick: () => ancestorClicked = true,
                onButtonClick: () => buttonClicked = true));

            cut.Find("button").Click();

            Assert.IsTrue(buttonClicked);
            Assert.IsTrue(ancestorClicked, "Without StopPropagation, a normal button must keep bubbling the click to its ancestor.");
        }

        private RenderFragment RenderButtonInsideClickableAncestor(
            bool stopPropagation, Action onAncestorClick, Action onButtonClick) => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, onAncestorClick));
            builder.OpenComponent<Button>(2);
            builder.AddAttribute(3, nameof(Button.StopPropagation), stopPropagation);
            builder.AddAttribute(4, nameof(Button.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, onButtonClick));
            builder.AddAttribute(5, nameof(Button.ChildContent), (RenderFragment)(childBuilder => childBuilder.AddContent(0, "Bearbeiten")));
            builder.CloseComponent();
            builder.CloseElement();
        };
    }
}
