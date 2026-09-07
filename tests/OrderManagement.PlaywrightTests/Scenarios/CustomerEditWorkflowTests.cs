using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed class CustomerEditWorkflowTests : PageTest
    {
        [TestMethod]
        public async Task ClickingEditButton_OpensOnlyEditDrawerAndSavingPersistsTheChange()
        {
            await Page.SetViewportSizeAsync(1280, 800);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/kunden");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator customerRow = Page.Locator("tbody tr", new() { HasText = PlaywrightSeedData.CustomerWithFutureMoveNumber });
            await Expect(customerRow).ToBeVisibleAsync();

            await customerRow.GetByRole(AriaRole.Button, new() { Name = "Bearbeiten" }).ClickAsync();

            ILocator editDrawer = Page.Locator("dialog[role=dialog]", new() { HasText = "Kunde bearbeiten" });
            await Expect(editDrawer).ToBeVisibleAsync();

            await Expect(Page.Locator("dialog[role=dialog]", new() { HasText = "Kundendetails" })).Not.ToBeVisibleAsync();
            await AssertRowNotSelectedAsync(customerRow);
            await Expect(Page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();

            string newEmail = $"maria.updated.{Guid.NewGuid():N}@example.com";
            await editDrawer.Locator("#cust-email").FillAsync(newEmail);
            await editDrawer.GetByRole(AriaRole.Button, new() { Name = "Änderungen speichern" }).ClickAsync();

            await Expect(Page.Locator(".inline-alert-success")).ToContainTextAsync("Kunde wurde aktualisiert.");
            await Expect(editDrawer).Not.ToBeVisibleAsync();
            await Expect(Page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();

            await Expect(Page.Locator("tbody")).ToContainTextAsync(newEmail);

            await customerRow.GetByRole(AriaRole.Button, new() { Name = "Bearbeiten" }).ClickAsync();
            await Expect(editDrawer).ToBeVisibleAsync();
            await Expect(editDrawer.Locator("#cust-email")).ToHaveValueAsync(newEmail);

            await Expect(Page.Locator("body")).ToBeVisibleAsync();
            await Expect(Page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();
        }

        [TestMethod]
        public async Task ClickingDeleteButton_DoesNotOpenCustomerDetailsOrSelectTheRow()
        {
            await Page.SetViewportSizeAsync(1280, 800);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/kunden");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator customerRow = Page.Locator("tbody tr", new() { HasText = PlaywrightSeedData.CustomerWithFutureMoveNumber });
            await Expect(customerRow).ToBeVisibleAsync();

            await customerRow.GetByRole(AriaRole.Button, new() { Name = "Löschen" }).ClickAsync();

            await Expect(Page.Locator("dialog[role=dialog]", new() { HasText = "Kunde löschen" })).ToBeVisibleAsync();
            await Expect(Page.Locator("dialog[role=dialog]", new() { HasText = "Kundendetails" })).Not.ToBeVisibleAsync();
            await AssertRowNotSelectedAsync(customerRow);

            await Page.GetByRole(AriaRole.Button, new() { Name = "Abbrechen" }).ClickAsync();
            await Expect(Page.Locator("#blazor-error-ui")).Not.ToBeVisibleAsync();
        }

        private static async Task AssertRowNotSelectedAsync(ILocator row)
        {
            string? cssClass = await row.GetAttributeAsync("class");
            Assert.IsFalse(
                cssClass?.Contains("is-selected", StringComparison.Ordinal) ?? false,
                "The row must not be marked selected by a click that only targeted a row action button.");
        }
    }
}
