using System.Text.RegularExpressions;

using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed partial class OrderDeletionTests : PageTest
    {
        [GeneratedRegex(@"/auftraege/\d+$")]
        private static partial Regex OrderDetailUrlRegex();

        [GeneratedRegex(@"/auftraege$")]
        private static partial Regex OrdersListUrlRegex();

        [TestMethod]
        public async Task DeletingOrderFromDetailPage_RestoresStock_RemovesOrder_AndOldUrlIsNotFound()
        {
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/auftraege");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator searchInput = Page.Locator(".toolbar input[type='search']");
            await searchInput.FillAsync(PlaywrightSeedData.DeletableOrderNumber);
            await searchInput.PressAsync("Enter");

            ILocator orderRow = Page.Locator("tbody tr", new() { HasText = PlaywrightSeedData.DeletableOrderNumber });
            await Expect(orderRow).ToBeVisibleAsync();
            await orderRow.ClickAsync();

            await Expect(Page).ToHaveURLAsync(OrderDetailUrlRegex());
            await Expect(Page.Locator(".page-header")).ToContainTextAsync(PlaywrightSeedData.DeletableOrderNumber);

            string detailUrl = Page.Url;

            ILocator deleteButton = Page.Locator("button", new() { HasText = "Auftrag löschen" });
            await deleteButton.ClickAsync();

            ILocator dialog = Page.Locator("dialog.app-modal");
            await Expect(dialog).ToBeVisibleAsync();
            await Expect(dialog).ToContainTextAsync($"«{PlaywrightSeedData.DeletableOrderNumber}»");
            await Expect(dialog).ToContainTextAsync("Lagerbestand wieder gutgeschrieben");
            await Expect(dialog).ToContainTextAsync("kann nicht rückgängig gemacht werden");

            await dialog.Locator("button", new() { HasText = "Löschen" }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(OrdersListUrlRegex());
            await Expect(Page.Locator(".data-table")).Not.ToContainTextAsync(PlaywrightSeedData.DeletableOrderNumber);

            _ = await Page.GotoAsync(detailUrl);
            await Page.WaitForBlazorInteractiveAsync();
            await Expect(Page.Locator(".feedback-state-error")).ToBeVisibleAsync();

            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/artikel");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator articleSearch = Page.Locator(".articles-toolbar input[type='search']");
            await articleSearch.FillAsync(PlaywrightSeedData.ReferencedArticleNumber);
            await articleSearch.PressAsync("Enter");

            ILocator articleRow = Page.Locator("tbody tr", new() { HasText = PlaywrightSeedData.ReferencedArticleNumber });
            await Expect(articleRow).ToContainTextAsync($"{PlaywrightSeedData.DeletableOrderArticleStockAfterDeletion} Stk.");
        }
    }
}
