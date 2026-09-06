using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed class CategorySelectorViewportTests : PageTest
    {
        private const int ViewportMargin = 8;

        [TestMethod]
        public async Task WideViewport_DrillingThroughFourLevels_KeepsEveryPanelInsideViewportBounds()
        {
            _ = await OpenArticlesToolbarSelectorAsync(1920, 1080);

            await Page.Locator(".category-flyout-item", new() { HasText = PlaywrightSeedData.RootCategoryName }).First.ClickAsync();
            await Page.Locator(".category-flyout-item", new() { HasText = "Kabel & Adapter" }).First.ClickAsync();
            await Page.Locator(".category-flyout-item", new() { HasText = "USB" }).First.ClickAsync();

            IReadOnlyList<ILocator> panels = await Page.Locator(".category-flyout-panel-wrapper").AllAsync();
            Assert.IsTrue(panels.Count >= 3, "Expected at least three cascading panels to be visible.");

            foreach (ILocator panel in panels)
            {
                await AssertWithinViewportAsync(panel);
            }
        }

        [TestMethod]
        public async Task DeepHierarchy_WhenRightwardCascadeCannotFit_SwitchesToCompactDrilldown()
        {
            _ = await OpenArticlesToolbarSelectorAsync(700, 800);

            await Page.Locator(".category-flyout-item", new() { HasText = PlaywrightSeedData.RootCategoryName }).First.ClickAsync();
            await Page.Locator(".category-flyout-item", new() { HasText = "Kabel & Adapter" }).First.ClickAsync();
            await Page.Locator(".category-flyout-item", new() { HasText = "USB" }).First.ClickAsync();

            ILocator drilldown = Page.Locator(".category-flyout-drilldown");
            await Expect(drilldown).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Expect(Page.Locator(".category-flyout-back")).ToBeVisibleAsync();
            await Expect(Page.Locator(".category-flyout-menu")).ToContainTextAsync("USB-C");

            await AssertWithinViewportAsync(Page.Locator(".category-flyout-panels"));
            Assert.AreEqual(0, await GetSelectorHorizontalOverflowAsync());
        }

        [TestMethod]
        public async Task ShortViewport_OpensAboveTriggerWhenNotEnoughSpaceBelow()
        {
            _ = await OpenArticlesToolbarSelectorAsync(1280, 420);

            ILocator panels = Page.Locator(".category-flyout-panels");
            await Expect(panels).ToBeVisibleAsync();
            await AssertWithinViewportAsync(panels);
        }

        [TestMethod]
        public async Task ResizingWhileOpen_RepositionsPanelsAndKeepsThemInsideTheNewViewport()
        {
            _ = await OpenArticlesToolbarSelectorAsync(1920, 1080);
            await Expect(Page.Locator(".category-flyout-panel-wrapper").First).ToBeVisibleAsync();

            await Page.SetViewportSizeAsync(1280, 720);
            await Page.WaitForTimeoutAsync(300);

            await Expect(Page.Locator(".category-flyout-panels")).ToBeVisibleAsync();
            await AssertWithinViewportAsync(Page.Locator(".category-flyout-panels"));
        }

        [TestMethod]
        public async Task Escape_ClosesTheSelectorAndReturnsFocusToTrigger()
        {
            ILocator trigger = await OpenArticlesToolbarSelectorAsync(1440, 900);

            ILocator firstItem = Page.Locator(".category-flyout-item").First;
            await firstItem.FocusAsync();
            await firstItem.PressAsync("Escape");

            await Expect(Page.Locator(".category-flyout-panels")).Not.ToBeVisibleAsync();
            await Expect(trigger).ToBeFocusedAsync();
        }

        [TestMethod]
        public async Task OutsideClick_ClosesTheSelector()
        {
            _ = await OpenArticlesToolbarSelectorAsync(1440, 900);
            await Expect(Page.Locator(".category-flyout-panels")).ToBeVisibleAsync();

            await Page.Locator("h1").First.ClickAsync();

            await Expect(Page.Locator(".category-flyout-panels")).Not.ToBeVisibleAsync();
        }

        [TestMethod]
        public async Task ArticleDrawer_CategoryTreePicker_StaysEntirelyInsideTheDrawerBounds()
        {
            ILocator drawer = await OpenArticleDrawerAsync(1440, 900);

            ILocator categoryTrigger = drawer.Locator("button", new() { HasText = "Kategorie wählen" });
            await categoryTrigger.ClickAsync();

            ILocator picker = drawer.Locator(".category-tree-picker");
            await Expect(picker).ToBeVisibleAsync();

            LocatorBoundingBoxResult? drawerBox = await drawer.BoundingBoxAsync();
            LocatorBoundingBoxResult? pickerBox = await picker.BoundingBoxAsync();
            Assert.IsNotNull(drawerBox);
            Assert.IsNotNull(pickerBox);

            Assert.IsTrue(pickerBox!.X >= drawerBox!.X - 1, "Category picker starts before the drawer's left edge.");
            Assert.IsTrue(pickerBox.X + pickerBox.Width <= drawerBox.X + drawerBox.Width + 1, "Category picker extends past the drawer's right edge.");
            Assert.IsTrue(pickerBox.Y >= drawerBox.Y - 1, "Category picker starts above the drawer's top edge.");
        }

        [TestMethod]
        public async Task ArticleDrawer_CategoryTreePicker_SelectionRequiresExplicitClickAndApply()
        {
            ILocator drawer = await OpenArticleDrawerAsync(1440, 900);

            await drawer.Locator("button", new() { HasText = "Kategorie wählen" }).ClickAsync();
            ILocator picker = drawer.Locator(".category-tree-picker");
            await Expect(picker).ToBeVisibleAsync();

            ILocator rootRow = picker.Locator(".category-tree-label", new() { HasText = PlaywrightSeedData.RootCategoryName }).First;
            await rootRow.HoverAsync();
            await Expect(picker.Locator(".category-tree-row.is-selected")).Not.ToBeVisibleAsync();

            await rootRow.ClickAsync();
            await Expect(picker.Locator(".category-tree-row.is-selected")).ToBeVisibleAsync();

            await drawer.Locator("button", new() { HasText = "Übernehmen" }).ClickAsync();
            await Expect(picker).Not.ToBeVisibleAsync();
            await Expect(drawer.Locator("button", new() { HasText = "Kategorie ändern" })).ToBeVisibleAsync();
        }

        [TestMethod]
        public async Task ArticleDrawer_AtPrimarySupportedViewport_IsFullScreen()
        {
            ILocator drawer = await OpenArticleDrawerAsync(1280, 800);

            LocatorBoundingBoxResult? drawerBox = await drawer.BoundingBoxAsync();
            Assert.IsNotNull(drawerBox);
            Assert.AreEqual(1280, Math.Round(drawerBox!.Width), 2, "Drawer width should fill the viewport at 1280x800.");
            Assert.AreEqual(800, Math.Round(drawerBox.Height), 2, "Drawer height should fill the viewport at 1280x800.");
            Assert.AreEqual(0, await GetDocumentHorizontalOverflowAsync());
        }

        [TestMethod]
        public async Task ArticleDrawer_AtWideViewport_OccupiesAtLeastTwoThirdsOfViewportWidth()
        {
            ILocator drawer = await OpenArticleDrawerAsync(1920, 1080);

            LocatorBoundingBoxResult? drawerBox = await drawer.BoundingBoxAsync();
            Assert.IsNotNull(drawerBox);
            Assert.IsTrue(drawerBox!.Width >= (1920 * 2.0 / 3.0) - 2, $"Expected drawer width >= two thirds of 1920, got {drawerBox.Width}.");
        }

        [TestMethod]
        public async Task ArticleDrawer_AtExactly1366_IsFullScreen()
        {
            ILocator drawer = await OpenArticleDrawerAsync(1366, 768);

            LocatorBoundingBoxResult? drawerBox = await drawer.BoundingBoxAsync();
            Assert.IsNotNull(drawerBox);
            Assert.AreEqual(1366, Math.Round(drawerBox!.Width), 2, "Drawer should be full-screen at the 1366px threshold.");
        }

        [TestMethod]
        public async Task ArticleDrawer_JustAbove1366_IsWideButNotFullScreen()
        {
            ILocator drawer = await OpenArticleDrawerAsync(1440, 900);

            LocatorBoundingBoxResult? drawerBox = await drawer.BoundingBoxAsync();
            Assert.IsNotNull(drawerBox);
            Assert.IsTrue(drawerBox!.Width < 1440, "Drawer should not be full-screen above the 1366px threshold.");
            Assert.IsTrue(drawerBox.Width >= (1440 * 2.0 / 3.0) - 2, "Drawer should still be at least two thirds of the viewport width.");
        }

        [TestMethod]
        [DataRow(1280, 800)]
        [DataRow(1366, 768)]
        [DataRow(1440, 900)]
        [DataRow(1920, 1080)]
        public async Task ArticlesToolbar_AtSupportedViewports_HasNoDocumentHorizontalOverflow(int width, int height)
        {
            await Page.SetViewportSizeAsync(width, height);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/artikel");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator trigger = Page.Locator(".toolbar .category-flyout-trigger");
            await Expect(trigger).ToBeEnabledAsync();

            Assert.AreEqual(0, await GetDocumentHorizontalOverflowAsync(), $"Unexpected horizontal overflow at {width}x{height}.");

            ILocator category = Page.Locator(".articles-toolbar .category-flyout");
            ILocator search = Page.Locator(".articles-toolbar .search-field");
            LocatorBoundingBoxResult? categoryBox = await category.BoundingBoxAsync();
            LocatorBoundingBoxResult? searchBox = await search.BoundingBoxAsync();
            Assert.IsNotNull(categoryBox);
            Assert.IsNotNull(searchBox);
            Assert.IsTrue(categoryBox!.X <= searchBox!.X, $"Category selector should be at or left of the search field at {width}x{height}.");
        }

        private async Task<ILocator> OpenArticleDrawerAsync(int width, int height)
        {
            await Page.SetViewportSizeAsync(width, height);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/artikel");
            await Page.WaitForBlazorInteractiveAsync();
            await Expect(Page.Locator(".toolbar .category-flyout-trigger")).ToBeEnabledAsync();

            await Page.Locator("button", new() { HasText = "Neuer Artikel" }).ClickAsync();

            ILocator drawer = Page.Locator("dialog.app-drawer");
            await Expect(drawer).ToBeVisibleAsync(new() { Timeout = 10_000 });

            return drawer;
        }

        private async Task<int> GetDocumentHorizontalOverflowAsync() => await Page.EvaluateAsync<int>(
            "() => Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth)");

        private async Task<ILocator> OpenArticlesToolbarSelectorAsync(int width, int height)
        {
            await Page.SetViewportSizeAsync(width, height);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/artikel");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator trigger = Page.Locator(".toolbar .category-flyout-trigger");
            await Expect(trigger).ToBeEnabledAsync();
            await trigger.ClickAsync();

            return trigger;
        }

        private async Task AssertWithinViewportAsync(ILocator locator)
        {
            LocatorBoundingBoxResult? box = await locator.BoundingBoxAsync();
            Assert.IsNotNull(box, "Expected element to have a bounding box.");

            PageViewportSizeResult viewport = Page.ViewportSize!;

            Assert.IsTrue(box!.X >= ViewportMargin - 1, $"left {box.X} is outside the safety margin.");
            Assert.IsTrue(box.Y >= ViewportMargin - 1, $"top {box.Y} is outside the safety margin.");
            Assert.IsTrue(box.X + box.Width <= viewport.Width - ViewportMargin + 1, $"right {box.X + box.Width} exceeds viewport width {viewport.Width}.");
            Assert.IsTrue(box.Y + box.Height <= viewport.Height - ViewportMargin + 1, $"bottom {box.Y + box.Height} exceeds viewport height {viewport.Height}.");
        }

        private async Task<int> GetSelectorHorizontalOverflowAsync() => await Page.EvaluateAsync<int>(
            """
            () => {
                const panels = document.querySelector('.category-flyout-panels');
                if (!panels) { return 0; }
                const rect = panels.getBoundingClientRect();
                const overflowRight = Math.max(0, rect.right - document.documentElement.clientWidth);
                const overflowLeft = Math.max(0, -rect.left);
                return Math.round(overflowRight + overflowLeft);
            }
            """);
    }
}
