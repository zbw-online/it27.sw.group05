using AngleSharp.Dom;

using Bunit;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.DTOs.Catalog;
using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class CategoryTreePickerTests : Bunit.TestContext
    {
        private static readonly ArticleGroupHierarchyDto[] Hierarchy =
        [
            new(1, "Elektronik", null, 0, "Elektronik"),
            new(2, "Kabel & Adapter", 1, 1, "Elektronik > Kabel & Adapter"),
            new(3, "USB", 2, 2, "Elektronik > Kabel & Adapter > USB"),
            new(4, "Möbel", null, 0, "Möbel"),
        ];

        [TestMethod]
        public void Render_CollapsesChildrenByDefault_WhenNoSelectionProvided()
        {
            IRenderedComponent<CategoryTreePicker> cut = Render();

            Assert.AreEqual(2, cut.FindAll(".category-tree-row").Count);
            Assert.AreEqual(0, cut.FindAll(".category-tree-row").Count(r => r.TextContent.Contains("Kabel", StringComparison.Ordinal)));
        }

        [TestMethod]
        public void ClickingChevron_ExpandsAndThenCollapsesChildren()
        {
            IRenderedComponent<CategoryTreePicker> cut = Render();

            cut.Find(".category-tree-toggle").Click();
            Assert.IsTrue(cut.FindAll(".category-tree-row").Any(r => r.TextContent.Contains("Kabel & Adapter", StringComparison.Ordinal)));

            cut.Find(".category-tree-toggle").Click();
            Assert.IsFalse(cut.FindAll(".category-tree-row").Any(r => r.TextContent.Contains("Kabel & Adapter", StringComparison.Ordinal)));
        }

        [TestMethod]
        public void ClickingRow_SetsPendingSelection_ButDoesNotInvokeOnApply()
        {
            int? applied = null;
            IRenderedComponent<CategoryTreePicker> cut = Render(onApply: id => applied = id);

            cut.Find(".category-tree-label").Click();

            Assert.IsNull(applied);
            StringAssert.Contains(cut.Find(".category-tree-picker-pending").TextContent, "Elektronik");
        }

        [TestMethod]
        public void ClickingUebernehmen_InvokesOnApplyWithPendingSelection()
        {
            int? applied = null;
            IRenderedComponent<CategoryTreePicker> cut = Render(onApply: id => applied = id);

            cut.Find(".category-tree-label").Click();
            FindButtonByText(cut, "Übernehmen").Click();

            Assert.AreEqual(1, applied);
        }

        [TestMethod]
        public void ClickingAbbrechen_InvokesOnCancel_WithoutApplying()
        {
            int? applied = null;
            bool cancelled = false;
            IRenderedComponent<CategoryTreePicker> cut = Render(onApply: id => applied = id, onCancel: () => cancelled = true);

            cut.Find(".category-tree-label").Click();
            FindButtonByText(cut, "Abbrechen").Click();

            Assert.IsNull(applied);
            Assert.IsTrue(cancelled);
        }

        [TestMethod]
        public void UebernehmenButton_IsDisabled_UntilARowIsSelected()
        {
            IRenderedComponent<CategoryTreePicker> cut = Render();

            Assert.IsTrue(FindButtonByText(cut, "Übernehmen").HasAttribute("disabled"));

            cut.Find(".category-tree-label").Click();

            Assert.IsFalse(FindButtonByText(cut, "Übernehmen").HasAttribute("disabled"));
        }

        [TestMethod]
        public void Search_MatchesByNameAndPath_AndShowsFullPathForDisambiguation()
        {
            IRenderedComponent<CategoryTreePicker> cut = Render();

            cut.Find("input[type=search]").Input("USB");

            IRefreshableElementCollection<IElement> results = cut.FindAll(".category-tree-search-result");
            Assert.AreEqual(1, results.Count);
            StringAssert.Contains(results[0].TextContent, "USB");
            StringAssert.Contains(results[0].TextContent, "Elektronik > Kabel & Adapter > USB");
        }

        [TestMethod]
        public void ExistingSelection_ExpandsAncestorsAndShowsCurrentPathAsPending()
        {
            IRenderedComponent<CategoryTreePicker> cut = Render(selectedGroupId: 3);

            Assert.IsTrue(cut.FindAll(".category-tree-row").Any(r => r.TextContent.Contains("USB", StringComparison.Ordinal)));
            StringAssert.Contains(cut.Find(".category-tree-picker-pending").TextContent, "Elektronik > Kabel & Adapter > USB");
        }

        private static IElement FindButtonByText(IRenderedComponent<CategoryTreePicker> cut, string text) =>
            cut.FindAll("button").Single(b => b.TextContent.Trim() == text);

        private IRenderedComponent<CategoryTreePicker> Render(
            int? selectedGroupId = null,
            Action<int>? onApply = null,
            Action? onCancel = null)
            => RenderComponent<CategoryTreePicker>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.SelectedGroupId, selectedGroupId)
                .Add(p => p.OnApply, onApply ?? (_ => { }))
                .Add(p => p.OnCancel, onCancel ?? (() => { })));
    }
}
