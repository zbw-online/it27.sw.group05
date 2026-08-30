using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class DateInputTests : Bunit.TestContext
    {
        public DateInputTests()
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [TestMethod]
        public void Render_ShowsSwissFormattedDate()
        {
            IRenderedComponent<DateInput> cut = RenderComponent<DateInput>(parameters => parameters
                .Add(p => p.Value, new DateTime(2026, 6, 9)));

            Assert.AreEqual("09.06.2026", cut.Find(".date-input-text").GetAttribute("value"));
        }

        [TestMethod]
        public void TypingValidSwissDate_RaisesValueChanged()
        {
            DateTime? changed = null;
            IRenderedComponent<DateInput> cut = RenderComponent<DateInput>(parameters => parameters
                .Add(p => p.Value, new DateTime(2026, 1, 1))
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTime>(this, v => changed = v)));

            cut.Find(".date-input-text").Change("09.06.2026");

            Assert.AreEqual(new DateTime(2026, 6, 9), changed);
        }

        [TestMethod]
        public void TypingInvalidText_DoesNotRaiseValueChanged()
        {
            DateTime? changed = null;
            IRenderedComponent<DateInput> cut = RenderComponent<DateInput>(parameters => parameters
                .Add(p => p.Value, new DateTime(2026, 1, 1))
                .Add(p => p.ValueChanged, EventCallback.Factory.Create<DateTime>(this, v => changed = v)));

            cut.Find(".date-input-text").Change("nicht ein datum");

            Assert.IsNull(changed);
        }

        [TestMethod]
        public void Render_HasNativePickerButton()
        {
            IRenderedComponent<DateInput> cut = RenderComponent<DateInput>(parameters => parameters
                .Add(p => p.Value, new DateTime(2026, 6, 9)));

            Assert.AreEqual(1, cut.FindAll(".date-input-icon").Count);
        }
    }
}
