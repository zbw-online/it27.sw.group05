using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed class CustomerAddressTimelineTests : PageTest
    {
        [TestMethod]
        public async Task OpeningCustomerRow_ShowsCurrentAndFutureAddressSections()
        {
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/kunden");
            await Expect(Page.Locator("tbody tr").First).ToBeVisibleAsync();
            await Page.WaitForTimeoutAsync(1500);
            await Page.Locator("tbody tr", new() { HasText = PlaywrightSeedData.CustomerWithFutureMoveNumber }).ClickAsync();

            ILocator header = Page.Locator(".customer-detail-header");
            await Expect(header).ToContainTextAsync(PlaywrightSeedData.CustomerWithFutureMoveNumber, new() { Timeout = 20_000 });

            ILocator futureSection = Page.Locator(".address-section", new() { HasText = "Zukünftige Adressen" });
            await Expect(futureSection).ToContainTextAsync("Neue Gasse");

            ILocator currentSection = Page.Locator(".address-section", new() { HasText = "Aktuelle Adresse" });
            await Expect(currentSection).ToContainTextAsync("Alte Gasse");
        }
    }
}
