using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed class BlazorInteractivitySmokeTests : PageTest
    {
        [TestMethod]
        public async Task Homepage_BecomesInteractive_AndRunsABlazorEventHandler()
        {
            _ = await Page.GotoAsync(PlaywrightAppFixture.BaseUrl);
            await Page.WaitForBlazorInteractiveAsync();

            ILocator newOrderButton = Page.Locator("button", new() { HasText = "Neuer Auftrag" });
            await Expect(newOrderButton).ToBeVisibleAsync();

            await newOrderButton.ClickAsync();

            await Expect(Page).ToHaveURLAsync($"{PlaywrightAppFixture.BaseUrl}/auftraege/neu");
        }
    }
}
