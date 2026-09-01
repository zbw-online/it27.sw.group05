using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.DTOs.Catalog;
using OrderManagement.Presentation.Blazor.Components.Shared;

namespace OrderManagement.Presentation.Blazor.Tests.Shared
{
    [TestClass]
    public sealed class CategoryCascadeSelectorTests : Bunit.TestContext
    {
        private static readonly ArticleGroupHierarchyDto[] Hierarchy =
        [
            new(1, "Bürobedarf", null, 0, "Bürobedarf"),
            new(2, "Werkzeuge", null, 0, "Werkzeuge"),
            new(10, "Schreibwaren", 1, 1, "Bürobedarf > Schreibwaren"),
            new(20, "Papier", 1, 1, "Bürobedarf > Papier"),
            new(100, "Kugelschreiber", 10, 2, "Bürobedarf > Schreibwaren > Kugelschreiber")
        ];

        public CategoryCascadeSelectorTests() => JSInterop.Mode = JSRuntimeMode.Loose;

        [TestMethod]
        public void Render_Closed_ShowsAllCategoriesAndNoPanels()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            Assert.AreEqual("Alle Kategorien", cut.Find(".category-flyout-value").TextContent);
            Assert.AreEqual(0, cut.FindAll(".category-flyout-panels").Count);
        }

        [TestMethod]
        public void ClickingTrigger_OpensMenuShowingOnlyRootLevelItems()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").Click();

            Assert.AreEqual(1, cut.FindAll(".category-flyout-panels").Count);
            Assert.AreEqual(1, cut.FindAll(".category-flyout-panel-wrapper").Count);

            string rootPanelText = cut.Find(".category-flyout-panel-wrapper").TextContent;
            StringAssert.Contains(rootPanelText, "Alle Kategorien");
            StringAssert.Contains(rootPanelText, "Bürobedarf");
            StringAssert.Contains(rootPanelText, "Werkzeuge");
        }

        [TestMethod]
        public void EnterOnTrigger_OpensMenu()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.AreEqual(1, cut.FindAll(".category-flyout-panels").Count);
        }

        [TestMethod]
        public void SelectingRootLevelWithChildren_RevealsItsDirectChildrenOnly()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Bürobedarf")).Click();

            Assert.AreEqual(2, cut.FindAll(".category-flyout-panel-wrapper").Count);
            string childPanelText = cut.FindAll(".category-flyout-panel-wrapper")[1].TextContent;
            StringAssert.Contains(childPanelText, "Schreibwaren");
            StringAssert.Contains(childPanelText, "Papier");
        }

        [TestMethod]
        public void SelectingLeaf_AppliesSelectionAndClosesMenu()
        {
            int? lastSelected = -1;
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.SelectedGroupIdChanged, id => lastSelected = id));

            cut.Find(".category-flyout-trigger").Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Werkzeuge")).Click();

            Assert.AreEqual(2, lastSelected);
            Assert.AreEqual(0, cut.FindAll(".category-flyout-panels").Count);
        }

        [TestMethod]
        public void SelectingLeaf_UpdatesClosedControlToShowFullPath()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.SelectedGroupId, 100));

            Assert.AreEqual("Bürobedarf > Schreibwaren > Kugelschreiber", cut.Find(".category-flyout-value").TextContent);
        }

        [TestMethod]
        public void SelectingAlleKategorien_ClearsSelection()
        {
            int? lastSelected = -1;
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.SelectedGroupId, 2)
                .Add(p => p.SelectedGroupIdChanged, id => lastSelected = id));

            cut.Find(".category-flyout-trigger").Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Alle Kategorien")).Click();

            Assert.IsNull(lastSelected);
        }

        [TestMethod]
        public void ClearButton_WhenSelectionActive_ResetsSelectionWithoutOpeningMenu()
        {
            int? lastSelected = -1;
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.SelectedGroupId, 2)
                .Add(p => p.SelectedGroupIdChanged, id => lastSelected = id));

            cut.Find(".category-flyout-clear").Click();

            Assert.IsNull(lastSelected);
        }

        [TestMethod]
        public void ClearButton_IsAbsent_WhenNoSelection()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            Assert.AreEqual(0, cut.FindAll(".category-flyout-clear").Count);
        }

        [TestMethod]
        public void ChangingParentSelection_DoesNotShowPreviouslySelectedDescendant()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Bürobedarf")).Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Schreibwaren")).Click();

            Assert.AreEqual(3, cut.FindAll(".category-flyout-panel-wrapper").Count);
            StringAssert.Contains(cut.FindAll(".category-flyout-panel-wrapper")[2].TextContent, "Kugelschreiber");

            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Werkzeuge")).Click();

            cut.Find(".category-flyout-trigger").Click();
            Assert.AreEqual(1, cut.FindAll(".category-flyout-panel-wrapper").Count);
        }

        [TestMethod]
        public void SelectedGroupIdParameter_PreselectsAncestorPathWhenOpened()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.SelectedGroupId, 100));

            cut.Find(".category-flyout-trigger").Click();

            IElement[] panels = [.. cut.FindAll(".category-flyout-panel-wrapper")];
            Assert.AreEqual(3, panels.Length);
            StringAssert.Contains(panels[0].TextContent, "Bürobedarf");
            StringAssert.Contains(panels[1].TextContent, "Schreibwaren");
            StringAssert.Contains(panels[2].TextContent, "Kugelschreiber");

            IElement selectedLeaf = cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Kugelschreiber"));
            Assert.IsTrue(selectedLeaf.ClassList.Contains("is-selected"));
        }

        [TestMethod]
        public void ArrowDown_MovesFocusToNextItem()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").Click();

            IElement[] rootItems = [.. cut.FindAll(".category-flyout-item")];
            rootItems[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            rootItems = [.. cut.FindAll(".category-flyout-item")];
            Assert.AreEqual("0", rootItems[1].GetAttribute("tabindex"));
            Assert.AreEqual("-1", rootItems[0].GetAttribute("tabindex"));
        }

        [TestMethod]
        public void ArrowRight_OnItemWithChildren_OpensChildPanelAndMovesFocusIntoIt()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").Click();
            IElement buero = cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Bürobedarf"));
            buero.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

            Assert.AreEqual(2, cut.FindAll(".category-flyout-panel-wrapper").Count);
        }

        [TestMethod]
        public void ArrowLeft_CollapsesChildPanelAndReturnsFocusToParent()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Bürobedarf")).Click();
            Assert.AreEqual(2, cut.FindAll(".category-flyout-panel-wrapper").Count);

            IElement schreibwaren = cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Schreibwaren"));
            schreibwaren.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });

            Assert.AreEqual(1, cut.FindAll(".category-flyout-panel-wrapper").Count);
        }

        [TestMethod]
        public void Escape_ClosesMenuWithoutChangingSelection()
        {
            int? lastSelected = -1;
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.SelectedGroupIdChanged, id => lastSelected = id));

            cut.Find(".category-flyout-trigger").Click();
            IElement item = cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Bürobedarf"));
            item.KeyDown(new KeyboardEventArgs { Key = "Escape" });

            Assert.AreEqual(0, cut.FindAll(".category-flyout-panels").Count);
            Assert.AreEqual(-1, lastSelected);
        }

        [TestMethod]
        public void EmptyHierarchy_ShowsEmptyState()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, []));

            cut.Find(".category-flyout-trigger").Click();

            StringAssert.Contains(cut.Find(".category-flyout-status").TextContent, "Keine Kategorien vorhanden");
        }

        [TestMethod]
        public void IsLoading_ShowsLoadingState()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.IsLoading, true));

            cut.Find(".category-flyout-trigger").Click();

            StringAssert.Contains(cut.Find(".category-flyout-status").TextContent, "Kategorien werden geladen");
        }

        [TestMethod]
        public void ArbitraryDepth_FourLevelHierarchy_OpensAllLevelsWhenDrillingDown()
        {
            ArticleGroupHierarchyDto[] deepHierarchy =
            [
                new(1, "Ebene-Root", null, 0, "Ebene-Root"),
                new(2, "Ebene-Zwei", 1, 1, "Ebene-Root > Ebene-Zwei"),
                new(3, "Ebene-Drei", 2, 2, "Ebene-Root > Ebene-Zwei > Ebene-Drei"),
                new(4, "Ebene-Vier", 3, 3, "Ebene-Root > Ebene-Zwei > Ebene-Drei > Ebene-Vier")
            ];

            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, deepHierarchy));

            cut.Find(".category-flyout-trigger").Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Ebene-Root")).Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Ebene-Zwei")).Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Ebene-Drei")).Click();

            Assert.AreEqual(4, cut.FindAll(".category-flyout-panel-wrapper").Count);
            StringAssert.Contains(cut.FindAll(".category-flyout-panel-wrapper")[3].TextContent, "Ebene-Vier");
        }

        [TestMethod]
        public void WhenPlacementCannotFitEitherSide_SwitchesToCompactSingleLevelDrillDown()
        {
            _ = JSInterop.Setup<bool>("applyPlacement", _ => true).SetResult(true);

            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").Click();

            cut.WaitForAssertion(
                () => Assert.AreEqual(1, cut.FindAll(".category-flyout-drilldown").Count),
                TimeSpan.FromSeconds(3));
            Assert.AreEqual(1, cut.FindAll(".category-flyout-menu").Count);
            Assert.AreEqual(0, cut.FindAll(".category-flyout-back").Count);
        }

        [TestMethod]
        public void WhenPlacementFitsOnASide_KeepsCascadingPanels()
        {
            _ = JSInterop.Setup<bool>("applyPlacement", _ => true).SetResult(false);

            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").Click();

            cut.WaitForAssertion(
                () => Assert.AreEqual(0, cut.FindAll(".category-flyout-drilldown").Count),
                TimeSpan.FromSeconds(3));
            Assert.AreEqual(1, cut.FindAll(".category-flyout-panel-wrapper").Count);
        }

        [TestMethod]
        public void CompactViewport_ShowsSingleLevelDrillDownWithBackButtonAndBreadcrumb()
        {
            _ = JSInterop.Setup<bool>("applyPlacement", _ => true).SetResult(true);

            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.Find(".category-flyout-trigger").Click();
            cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindAll(".category-flyout-drilldown").Count), TimeSpan.FromSeconds(3));

            Assert.AreEqual(1, cut.FindAll(".category-flyout-menu").Count);
            Assert.AreEqual(0, cut.FindAll(".category-flyout-back").Count);
            StringAssert.Contains(cut.Find(".category-flyout-drilldown-breadcrumb").TextContent, "Alle Kategorien");

            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Bürobedarf")).Click();

            Assert.AreEqual(1, cut.FindAll(".category-flyout-menu").Count);
            Assert.AreEqual(1, cut.FindAll(".category-flyout-back").Count);
            StringAssert.Contains(cut.Find(".category-flyout-drilldown-breadcrumb").TextContent, "Bürobedarf");
            StringAssert.Contains(cut.Find(".category-flyout-menu").TextContent, "Schreibwaren");

            cut.Find(".category-flyout-back").Click();

            Assert.AreEqual(0, cut.FindAll(".category-flyout-back").Count);
            StringAssert.Contains(cut.Find(".category-flyout-menu").TextContent, "Bürobedarf");
        }

        [TestMethod]
        public void InlineDrilldownMode_NeverOpensAsOverlayAndStaysCompact()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.Mode, CategorySelectorMode.InlineDrilldown));

            cut.Find(".category-flyout-trigger").Click();

            Assert.AreEqual(1, cut.FindAll(".category-flyout-panels.is-inline").Count);
            Assert.AreEqual(1, cut.FindAll(".category-flyout-drilldown").Count);
            Assert.AreEqual(0, cut.FindAll(".category-flyout-panel-wrapper").Count);
        }

        [TestMethod]
        public void InlineDrilldownMode_SupportsArbitraryDepthViaBackNavigation()
        {
            ArticleGroupHierarchyDto[] deepHierarchy =
            [
                new(1, "Ebene-Root", null, 0, "Ebene-Root"),
                new(2, "Ebene-Zwei", 1, 1, "Ebene-Root > Ebene-Zwei"),
                new(3, "Ebene-Drei", 2, 2, "Ebene-Root > Ebene-Zwei > Ebene-Drei")
            ];

            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, deepHierarchy)
                .Add(p => p.Mode, CategorySelectorMode.InlineDrilldown));

            cut.Find(".category-flyout-trigger").Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Ebene-Root")).Click();
            cut.FindAll(".category-flyout-item").First(e => e.TextContent.Contains("Ebene-Zwei")).Click();

            StringAssert.Contains(cut.Find(".category-flyout-drilldown-breadcrumb").TextContent, "Ebene-Zwei");
            StringAssert.Contains(cut.Find(".category-flyout-menu").TextContent, "Ebene-Drei");

            cut.Find(".category-flyout-back").Click();
            StringAssert.Contains(cut.Find(".category-flyout-drilldown-breadcrumb").TextContent, "Ebene-Root");
            StringAssert.Contains(cut.Find(".category-flyout-menu").TextContent, "Ebene-Zwei");
        }
    }
}
