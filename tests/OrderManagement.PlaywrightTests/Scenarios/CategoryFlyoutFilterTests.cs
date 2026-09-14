using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed class CategoryFlyoutFilterTests : PageTest
    {
        [TestMethod]
        public async Task ClickNavigation_DrillsIntoDeepHierarchy_AndFiltersArticles()
        {
            ILocator trigger = await OpenCategorySelectorAsync();
            ILocator panels = Page.Locator(".category-flyout-panels");

            ILocator root = panels.GetByRole(AriaRole.Menuitem, new() { Name = PlaywrightSeedData.RootCategoryName, Exact = true });
            await Expect(root).ToBeVisibleAsync();
            await root.ClickAsync();

            ILocator level2 = panels.GetByRole(AriaRole.Menuitem, new() { Name = "Kabel & Adapter", Exact = true });
            await Expect(level2).ToBeVisibleAsync();
            await level2.ClickAsync();

            ILocator level3 = panels.GetByRole(AriaRole.Menuitem, new() { Name = "USB", Exact = true });
            await Expect(level3).ToBeVisibleAsync();
            await level3.ClickAsync();

            ILocator level4 = panels.GetByRole(AriaRole.Menuitem, new() { Name = "USB-C", Exact = true });
            await Expect(level4).ToBeVisibleAsync();
            await level4.ClickAsync();

            await Expect(Page.Locator(".toolbar .category-flyout-value")).ToContainTextAsync("USB-C");
            await Expect(Page.GetByText(PlaywrightSeedData.ReferencedArticleNumber)).ToBeVisibleAsync();
            await Expect(trigger).ToBeFocusedAsync();
        }

        [TestMethod]
        public async Task KeyboardNavigation_DrillsIntoDeepHierarchy_AndFiltersArticles()
        {
            ILocator trigger = await OpenCategorySelectorViaKeyboardAsync();
            ILocator panels = Page.Locator(".category-flyout-panels");

            ILocator allCategories = panels.GetByRole(AriaRole.Menuitem, new() { Name = "Alle Kategorien", Exact = true });
            await Expect(allCategories).ToBeFocusedAsync();

            ILocator root = panels.GetByRole(AriaRole.Menuitem, new() { Name = PlaywrightSeedData.RootCategoryName, Exact = true });
            await Page.Keyboard.PressAsync("ArrowDown");
            await Expect(root).ToBeFocusedAsync();

            await Page.Keyboard.PressAsync("ArrowUp");
            await Expect(allCategories).ToBeFocusedAsync();

            await Page.Keyboard.PressAsync("ArrowDown");
            await Expect(root).ToBeFocusedAsync();

            ILocator level2 = panels.GetByRole(AriaRole.Menuitem, new() { Name = "Kabel & Adapter", Exact = true });
            await Page.Keyboard.PressAsync("ArrowRight");
            await Expect(level2).ToBeFocusedAsync();

            ILocator level3 = panels.GetByRole(AriaRole.Menuitem, new() { Name = "USB", Exact = true });
            await Page.Keyboard.PressAsync("ArrowRight");
            await Expect(level3).ToBeFocusedAsync();

            ILocator level4 = panels.GetByRole(AriaRole.Menuitem, new() { Name = "USB-C", Exact = true });
            await Page.Keyboard.PressAsync("ArrowRight");
            await Expect(level4).ToBeVisibleAsync();
            await Expect(level4).ToBeFocusedAsync();

            await Page.Keyboard.PressAsync("ArrowLeft");
            await Expect(level3).ToBeFocusedAsync();
            await Expect(level4).Not.ToBeVisibleAsync();

            await Page.Keyboard.PressAsync("ArrowRight");
            await Expect(level4).ToBeFocusedAsync();

            await Page.Keyboard.PressAsync("Enter");

            await Expect(panels).Not.ToBeVisibleAsync();
            await Expect(Page.Locator(".toolbar .category-flyout-value")).ToContainTextAsync("USB-C");
            await Expect(Page.GetByText(PlaywrightSeedData.ReferencedArticleNumber)).ToBeVisibleAsync();
            await Expect(trigger).ToBeFocusedAsync();

            await Page.Keyboard.PressAsync("Enter");
            await Expect(panels).ToBeVisibleAsync();
            await Page.Keyboard.PressAsync("Escape");
            await Expect(panels).Not.ToBeVisibleAsync();
            await Expect(trigger).ToBeFocusedAsync();
            await Expect(Page.Locator(".toolbar .category-flyout-value")).ToContainTextAsync("USB-C");
        }

        private async Task<ILocator> OpenCategorySelectorAsync()
        {
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/artikel");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator trigger = Page.Locator(".toolbar .category-flyout-trigger");
            await Expect(trigger).ToBeEnabledAsync();
            await trigger.ClickAsync();

            await Expect(Page.Locator(".category-flyout-panels")).ToBeVisibleAsync();
            return trigger;
        }

        private async Task<ILocator> OpenCategorySelectorViaKeyboardAsync()
        {
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/artikel");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator trigger = Page.Locator(".toolbar .category-flyout-trigger");
            await Expect(trigger).ToBeEnabledAsync();
            await trigger.FocusAsync();
            await trigger.PressAsync("Enter");

            await Expect(Page.Locator(".category-flyout-panels")).ToBeVisibleAsync();
            return trigger;
        }
    }
}
