using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed class OrderWizardResponsiveTests : PageTest
    {
        [TestMethod]
        [DataRow(1280, 720, DisplayName = "1280x720 stacked")]
        [DataRow(1366, 768, DisplayName = "1366x768 stacked")]
        public async Task NarrowerLaptopViewports_StackPanelsInCustomerCatalogueSummaryOrder(int width, int height)
        {
            await GoToPositionsStepAsync(width, height);

            int columnCount = await GetWizardColumnCountAsync();
            Assert.AreEqual(1, columnCount, "Expected the wizard to stack into a single column at this width.");

            await AssertCustomerBeforeCatalogueBeforeSummaryAsync();
            await AssertSummaryIsNotStickyAsync();
            Assert.AreEqual(0, await GetDocumentHorizontalOverflowAsync());
        }

        [TestMethod]
        public async Task WideDesktopViewport_KeepsThreeColumnLayout()
        {
            await GoToPositionsStepAsync(1920, 1080);

            int columnCount = await GetWizardColumnCountAsync();
            Assert.AreEqual(3, columnCount, "Expected the wizard to keep three columns at this width.");

            Assert.AreEqual(0, await GetDocumentHorizontalOverflowAsync());
        }

        [TestMethod]
        [DataRow(1440, 900)]
        [DataRow(1536, 864)]
        public async Task MidSizeLaptopViewports_LayoutMatchesActualUsableContainerWidth(int width, int height)
        {
            await GoToPositionsStepAsync(width, height);

            int containerWidth = await Page.EvaluateAsync<int>(
                "() => Math.round(document.querySelector('.wizard-content').getBoundingClientRect().width)");
            int columnCount = await GetWizardColumnCountAsync();

            int expectedColumnCount = containerWidth >= 1240 ? 3 : 1;
            Assert.AreEqual(expectedColumnCount, columnCount,
                $"Container width was {containerWidth}px; expected {expectedColumnCount} column(s) based on the 1240px container-query threshold.");

            if (expectedColumnCount == 1)
            {
                await AssertCustomerBeforeCatalogueBeforeSummaryAsync();
                await AssertSummaryIsNotStickyAsync();
            }

            Assert.AreEqual(0, await GetDocumentHorizontalOverflowAsync());
        }

        [TestMethod]
        public async Task StackedLayout_CanStillCompleteAnOrder()
        {
            await GoToPositionsStepAsync(1280, 720);

            ILocator addButton = Page.Locator(".data-table tbody tr", new() { HasText = PlaywrightSeedData.ReferencedArticleNumber })
                .Locator("button", new() { HasText = "Hinzufügen" });
            await Expect(addButton).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await addButton.ClickAsync();

            ILocator cartLine = Page.Locator(".cart-line", new() { HasText = PlaywrightSeedData.ReferencedArticleNumber });
            await Expect(cartLine).ToBeVisibleAsync();

            ILocator quantityInput = cartLine.Locator(".quantity-input");
            await Expect(quantityInput).ToBeVisibleAsync();

            ILocator continueButton = Page.Locator("button", new() { HasText = "Weiter zur Prüfung" });
            await Expect(continueButton).ToBeEnabledAsync(new() { Timeout = 10_000 });
        }

        private async Task GoToPositionsStepAsync(int width, int height)
        {
            await Page.SetViewportSizeAsync(width, height);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/auftraege/neu");

            ILocator searchField = Page.Locator("input[type=search]").First;
            await Expect(searchField).ToBeEnabledAsync();
            await Page.WaitForTimeoutAsync(1500);

            await searchField.FillAsync(PlaywrightSeedData.CustomerWithFutureMoveNumber);
            await searchField.PressAsync("Enter");

            ILocator selectButton = Page.Locator("button", new() { HasText = "Auswählen" }).First;
            await Expect(selectButton).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await selectButton.ClickAsync();

            ILocator nextButton = Page.Locator("button", new() { HasText = "Weiter zu Positionen" });
            await Expect(nextButton).ToBeEnabledAsync();
            await nextButton.ClickAsync();

            await Expect(Page.Locator(".wizard-columns")).ToBeVisibleAsync(new() { Timeout = 10_000 });
        }

        private async Task<int> GetWizardColumnCountAsync() => await Page.EvaluateAsync<int>(
            """
            () => {
                const el = document.querySelector('.wizard-columns');
                const template = getComputedStyle(el).gridTemplateColumns;
                return template.split(' ').filter(Boolean).length;
            }
            """);

        private async Task AssertCustomerBeforeCatalogueBeforeSummaryAsync()
        {
            ILocator customerPanel = Page.Locator(".wizard-columns .panel", new() { HasText = "Kunde" }).First;
            ILocator cataloguePanel = Page.Locator(".wizard-columns .panel", new() { HasText = "Artikel hinzufügen" });
            ILocator summaryPanel = Page.Locator(".wizard-summary");

            LocatorBoundingBoxResult? customerBox = await customerPanel.BoundingBoxAsync();
            LocatorBoundingBoxResult? catalogueBox = await cataloguePanel.BoundingBoxAsync();
            LocatorBoundingBoxResult? summaryBox = await summaryPanel.BoundingBoxAsync();

            Assert.IsNotNull(customerBox);
            Assert.IsNotNull(catalogueBox);
            Assert.IsNotNull(summaryBox);

            Assert.IsTrue(customerBox.Y < catalogueBox.Y, "Customer panel should be above the catalogue panel when stacked.");
            Assert.IsTrue(catalogueBox.Y < summaryBox.Y, "Catalogue panel should be above the summary panel when stacked.");
        }

        private async Task AssertSummaryIsNotStickyAsync()
        {
            string position = await Page.EvaluateAsync<string>(
                "() => getComputedStyle(document.querySelector('.wizard-summary')).position");
            Assert.AreNotEqual("sticky", position);
        }

        private async Task<int> GetDocumentHorizontalOverflowAsync() => await Page.EvaluateAsync<int>(
            "() => document.documentElement.scrollWidth - document.documentElement.clientWidth");
    }
}
