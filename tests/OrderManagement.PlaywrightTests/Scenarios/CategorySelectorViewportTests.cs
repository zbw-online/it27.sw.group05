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
        public async Task DeepHierarchy_WhenCascadeCannotFitOnEitherSide_SwitchesToCompactDrilldown()
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
        public async Task ArticleDrawer_CategorySelector_StaysEntirelyInsideTheDrawerBounds()
        {
            await Page.SetViewportSizeAsync(1440, 900);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/artikel");
            await Expect(Page.Locator(".toolbar .category-flyout-trigger")).ToBeEnabledAsync();
            await Page.WaitForTimeoutAsync(1500);

            await Page.Locator("button", new() { HasText = "Neuer Artikel" }).ClickAsync();

            ILocator drawer = Page.Locator("dialog.app-drawer");
            await Expect(drawer).ToBeVisibleAsync(new() { Timeout = 10_000 });

            ILocator categoryTrigger = drawer.Locator(".category-flyout-trigger");
            await categoryTrigger.ClickAsync();

            ILocator inlinePanels = drawer.Locator(".category-flyout-panels.is-inline");
            await Expect(inlinePanels).ToBeVisibleAsync();

            LocatorBoundingBoxResult? drawerBox = await drawer.BoundingBoxAsync();
            LocatorBoundingBoxResult? panelsBox = await inlinePanels.BoundingBoxAsync();
            Assert.IsNotNull(drawerBox);
            Assert.IsNotNull(panelsBox);

            Assert.IsTrue(panelsBox!.X >= drawerBox!.X - 1, "Category panel starts before the drawer's left edge.");
            Assert.IsTrue(panelsBox.X + panelsBox.Width <= drawerBox.X + drawerBox.Width + 1, "Category panel extends past the drawer's right edge.");
            Assert.IsTrue(panelsBox.Y >= drawerBox.Y - 1, "Category panel starts above the drawer's top edge.");
        }

        private async Task<ILocator> OpenArticlesToolbarSelectorAsync(int width, int height)
        {
            await Page.SetViewportSizeAsync(width, height);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/artikel");

            ILocator trigger = Page.Locator(".toolbar .category-flyout-trigger");
            await Expect(trigger).ToBeEnabledAsync();
            await Page.WaitForTimeoutAsync(1500);
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
