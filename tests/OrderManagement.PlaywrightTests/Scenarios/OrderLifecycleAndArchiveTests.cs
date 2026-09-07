using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed class OrderLifecycleAndArchiveTests : PageTest
    {
        [TestMethod]
        public async Task OrderLifecycle_FromOverdueOpenThroughProcessingToCompleted_MovesFromActiveListIntoArchive()
        {
            // 1. Open the orders page: only the active (laufend) list is shown, the archive is not queried yet.
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/auftraege");
            await Page.WaitForBlazorInteractiveAsync();

            await Expect(Page.Locator(".page-header")).ToContainTextAsync("Laufende Aufträge");

            ILocator activeSearchInput = Page.Locator(".toolbar input[type='search']").First;
            await activeSearchInput.FillAsync(PlaywrightSeedData.LifecycleOrderNumber);
            await activeSearchInput.PressAsync("Enter");

            // 2. The seed order starts Open with a past delivery date: visible, laufend, and overdue.
            ILocator activeRow = Page.Locator("tbody tr", new() { HasText = PlaywrightSeedData.LifecycleOrderNumber });
            await Expect(activeRow).ToBeVisibleAsync();
            await Expect(activeRow).ToContainTextAsync("Offen");
            await Expect(activeRow).ToContainTextAsync("Überfällig");

            ILocator archiveToggle = Page.Locator("button.archive-toggle");
            await Expect(archiveToggle).ToContainTextAsync("Archivierte Aufträge anzeigen");

            // 3. Set the order in progress and complete it from its detail page.
            await activeRow.ClickAsync();
            await Expect(Page.Locator(".page-header")).ToContainTextAsync(PlaywrightSeedData.LifecycleOrderNumber);

            await Page.Locator("button", new() { HasText = "Bearbeitung starten" }).ClickAsync();
            await Expect(Page.Locator(".order-status")).ToContainTextAsync("In Bearbeitung");

            await Page.Locator("button", new() { HasText = "Auftrag abschliessen" }).ClickAsync();
            await Expect(Page.Locator(".order-status")).ToContainTextAsync("Abgeschlossen");

            // 4. Back on the orders page, the now-completed order has left the active list ...
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/auftraege");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator activeSearchAgain = Page.Locator(".toolbar input[type='search']").First;
            await activeSearchAgain.FillAsync(PlaywrightSeedData.LifecycleOrderNumber);
            await activeSearchAgain.PressAsync("Enter");
            await Expect(Page.Locator(".feedback-state-empty")).ToBeVisibleAsync();

            // 5. ... and only appears once the archive is explicitly opened and searched.
            await Page.Locator("button.archive-toggle").ClickAsync();

            ILocator archiveSearchInput = Page.Locator(".toolbar input[type='search']").Nth(1);
            await archiveSearchInput.FillAsync(PlaywrightSeedData.LifecycleOrderNumber);
            await archiveSearchInput.PressAsync("Enter");

            ILocator archivedRow = Page.Locator("tbody tr", new() { HasText = PlaywrightSeedData.LifecycleOrderNumber });
            await Expect(archivedRow).ToBeVisibleAsync();
            await Expect(archivedRow).ToContainTextAsync("Abgeschlossen");
        }
    }
}
