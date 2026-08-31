using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed class CategoryFlyoutFilterTests : PageTest
    {
        [TestMethod]
        public async Task KeyboardNavigation_DrillsIntoDeepHierarchy_AndFiltersArticles()
        {
            Page.SetDefaultTimeout(20_000);

            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/artikel");

            ILocator trigger = Page.Locator(".toolbar .category-flyout-trigger");
            await Expect(trigger).ToBeEnabledAsync();
            await Page.WaitForTimeoutAsync(1500);
            await trigger.ClickAsync();

            ILocator rootItem = Page.Locator(".category-flyout-item", new() { HasText = PlaywrightSeedData.RootCategoryName }).First;
            await Expect(rootItem).ToBeVisibleAsync();
            await rootItem.ClickAsync();

            ILocator level2Item = Page.Locator(".category-flyout-item", new() { HasText = "Kabel & Adapter" }).First;
            await level2Item.ClickAsync();

            ILocator level3Item = Page.Locator(".category-flyout-item", new() { HasText = "USB" }).First;
            await level3Item.ClickAsync();

            ILocator level4Item = Page.Locator(".category-flyout-item", new() { HasText = "USB-C" }).First;
            await level4Item.ClickAsync();

            await Expect(Page.Locator(".toolbar .category-flyout-value")).ToContainTextAsync("USB-C");
            await Expect(Page.GetByText(PlaywrightSeedData.ReferencedArticleNumber)).ToBeVisibleAsync();
        }
    }
}
