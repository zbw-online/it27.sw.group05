using AngleSharp.Dom;

using Bunit;

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

        [TestMethod]
        public void Render_Initially_ShowsOnlyRootLevelItems()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            IElement rootColumn = cut.FindAll(".category-cascade-column")[0];
            Assert.IsTrue(rootColumn.TextContent.Contains("Bürobedarf"));
            Assert.IsTrue(rootColumn.TextContent.Contains("Werkzeuge"));

            IElement childColumn = cut.FindAll(".category-cascade-column")[1];
            Assert.IsTrue(childColumn.QuerySelectorAll("button").Length == 0);
        }

        [TestMethod]
        public void SelectingRootLevel_RevealsItsDirectChildrenOnly()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.FindAll(".category-cascade-item")
                .First(e => e.TextContent.Contains("Bürobedarf"))
                .Click();

            IElement childColumn = cut.FindAll(".category-cascade-column")[1];
            Assert.IsTrue(childColumn.TextContent.Contains("Schreibwaren"));
            Assert.IsTrue(childColumn.TextContent.Contains("Papier"));
        }

        [TestMethod]
        public void SelectingLeaf_BuildsBreadcrumbWithGreaterThanSeparator()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy));

            cut.FindAll(".category-cascade-item").First(e => e.TextContent.Contains("Bürobedarf")).Click();
            cut.FindAll(".category-cascade-item").First(e => e.TextContent.Contains("Schreibwaren")).Click();

            string breadcrumb = cut.Find(".category-cascade-breadcrumb").TextContent;
            Assert.IsTrue(breadcrumb.Contains("Bürobedarf"));
            Assert.IsTrue(breadcrumb.Contains("Schreibwaren"));
        }

        [TestMethod]
        public void ChangingParentSelection_ClearsPreviouslySelectedDescendant()
        {
            int? lastSelected = -1;
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.SelectedGroupIdChanged, id => lastSelected = id));

            cut.FindAll(".category-cascade-item").First(e => e.TextContent.Contains("Bürobedarf")).Click();
            cut.FindAll(".category-cascade-item").First(e => e.TextContent.Contains("Schreibwaren")).Click();
            cut.FindAll(".category-cascade-item").First(e => e.TextContent.Contains("Kugelschreiber")).Click();

            Assert.AreEqual(100, lastSelected);

            cut.FindAll(".category-cascade-item").First(e => e.TextContent.Contains("Werkzeuge")).Click();

            Assert.AreEqual(2, lastSelected);
            IElement kategorieColumn = cut.FindAll(".category-cascade-column")[2];
            Assert.IsFalse(kategorieColumn.TextContent.Contains("Kugelschreiber"));
        }

        [TestMethod]
        public void SelectedGroupIdParameter_PreselectsAncestorPath()
        {
            IRenderedComponent<CategoryCascadeSelector> cut = RenderComponent<CategoryCascadeSelector>(parameters => parameters
                .Add(p => p.Hierarchy, Hierarchy)
                .Add(p => p.SelectedGroupId, 100));

            string breadcrumb = cut.Find(".category-cascade-breadcrumb").TextContent;
            Assert.IsTrue(breadcrumb.Contains("Bürobedarf"));
            Assert.IsTrue(breadcrumb.Contains("Schreibwaren"));
            Assert.IsTrue(breadcrumb.Contains("Kugelschreiber"));
        }
    }
}
