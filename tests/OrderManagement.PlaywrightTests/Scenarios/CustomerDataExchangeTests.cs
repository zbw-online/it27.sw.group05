using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

using OrderManagement.PlaywrightTests.Support;

namespace OrderManagement.PlaywrightTests.Scenarios
{
    [TestClass]
    public sealed class CustomerDataExchangeTests : PageTest
    {
        // Safely past any real wall-clock time this suite could run at, so the SQL Server temporal
        // query always finds rows created "now" during assembly seeding, independent of the app's
        // clock (pinned to PlaywrightSeedData.ReferenceNow via Testing__FixedUtcNow).
        private const string FarFutureStichtag = "2099-01-01T12:00";

        [TestMethod]
        public async Task ExportingAsJson_DownloadsFileContainingCurrentCustomerData()
        {
            await Page.SetViewportSizeAsync(1280, 800);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/kunden");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator dialog = await OpenExportDialogAsync();
            await dialog.Locator("#export-stichtag").FillAsync(FarFutureStichtag);

            IDownload download = await Page.RunAndWaitForDownloadAsync(
                async () => await dialog.Locator("button", new() { HasText = "Exportieren" }).ClickAsync());

            StringAssert.EndsWith(download.SuggestedFilename, ".json");

            string path = await download.PathAsync() ?? throw new InvalidOperationException("Download path missing.");
            string content = await File.ReadAllTextAsync(path);
            StringAssert.Contains(content, PlaywrightSeedData.CustomerWithFutureMoveNumber);
            StringAssert.Contains(content, "Neue Gasse");
        }

        [TestMethod]
        public async Task ExportingAsXml_DownloadsFileContainingCurrentCustomerData()
        {
            await Page.SetViewportSizeAsync(1280, 800);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/kunden");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator dialog = await OpenExportDialogAsync();
            await dialog.Locator("input[value=Xml]").CheckAsync();
            await dialog.Locator("#export-stichtag").FillAsync(FarFutureStichtag);

            IDownload download = await Page.RunAndWaitForDownloadAsync(
                async () => await dialog.Locator("button", new() { HasText = "Exportieren" }).ClickAsync());

            StringAssert.EndsWith(download.SuggestedFilename, ".xml");

            string path = await download.PathAsync() ?? throw new InvalidOperationException("Download path missing.");
            string content = await File.ReadAllTextAsync(path);
            StringAssert.Contains(content, PlaywrightSeedData.CustomerWithFutureMoveNumber);
            StringAssert.Contains(content, "Neue Gasse");
        }

        [TestMethod]
        public async Task ImportingAValidJsonFile_CreatesTheCustomerAndReloadsTheList()
        {
            const string customerNumber = "CU00091";
            string filePath = WriteTempFile(
                "kunden-playwright-valid.json",
                $$"""
                [
                  {
                    "customerNumber": "{{customerNumber}}",
                    "lastName": "Spielmann",
                    "surName": "Petra",
                    "email": "petra.spielmann@example.ch",
                    "website": null,
                    "address": null
                  }
                ]
                """);

            try
            {
                await Page.SetViewportSizeAsync(1280, 800);
                _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/kunden");
                await Page.WaitForBlazorInteractiveAsync();

                ILocator dialog = await OpenImportDialogAsync();
                await dialog.Locator("input[type=file]").SetInputFilesAsync(filePath);
                await dialog.Locator("button", new() { HasText = "Datei prüfen" }).ClickAsync();
                await Expect(dialog.Locator(".inline-alert-success")).ToContainTextAsync("bereit zum Import");

                await dialog.Locator("button", new() { HasText = "In Datenbank importieren" }).ClickAsync();

                await Expect(Page.Locator(".inline-alert-success")).ToContainTextAsync("1 Kunden wurden importiert.");
                await Expect(Page.Locator("tbody")).ToContainTextAsync(customerNumber);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [TestMethod]
        public async Task ImportingAFileWithAnInvalidCustomer_ShowsIssuesAndKeepsImportDisabled()
        {
            string filePath = WriteTempFile(
                "kunden-playwright-invalid.json",
                                     /*lang=json,strict*/
                                     """
                [
                  {
                    "customerNumber": "not-a-number",
                    "lastName": "Ungueltig",
                    "surName": "Kunde",
                    "email": "invalid@example.ch",
                    "website": null,
                    "address": null
                  }
                ]
                """);

            try
            {
                await Page.SetViewportSizeAsync(1280, 800);
                _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/kunden");
                await Page.WaitForBlazorInteractiveAsync();

                ILocator customerCountBefore = Page.Locator(".page-subtitle");
                string subtitleBefore = await customerCountBefore.TextContentAsync() ?? string.Empty;

                ILocator dialog = await OpenImportDialogAsync();
                await dialog.Locator("input[type=file]").SetInputFilesAsync(filePath);
                await dialog.Locator("button", new() { HasText = "Datei prüfen" }).ClickAsync();

                await Expect(dialog.Locator(".validation-issue-list")).ToBeVisibleAsync();
                await Expect(dialog.Locator("button", new() { HasText = "In Datenbank importieren" })).ToBeDisabledAsync();

                await dialog.Locator("button", new() { HasText = "Abbrechen" }).ClickAsync();
                await Expect(Page.Locator(".page-subtitle")).ToHaveTextAsync(subtitleBefore);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [TestMethod]
        public async Task ImportDialog_Escape_ClosesDialogAndReturnsFocusToOpeningButton()
        {
            await Page.SetViewportSizeAsync(1280, 800);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/kunden");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator trigger = Page.Locator("button", new() { HasText = "Kundendaten importieren" });
            await trigger.ClickAsync();

            ILocator dialog = Page.Locator(".import-customer-dialog-host dialog");
            await Expect(dialog).ToBeVisibleAsync();

            await Page.Keyboard.PressAsync("Escape");

            await Expect(dialog).Not.ToBeVisibleAsync();
            await Expect(trigger).ToBeFocusedAsync();
        }

        [TestMethod]
        public async Task ExportDialog_AtPrimarySupportedViewport_StaysWithinViewportWithNoHorizontalOverflow()
        {
            await Page.SetViewportSizeAsync(1280, 800);
            _ = await Page.GotoAsync($"{PlaywrightAppFixture.BaseUrl}/kunden");
            await Page.WaitForBlazorInteractiveAsync();

            ILocator dialog = await OpenExportDialogAsync();

            LocatorBoundingBoxResult? box = await dialog.Locator("dialog").BoundingBoxAsync();
            Assert.IsNotNull(box, "Expected the dialog to have a bounding box.");

            PageViewportSizeResult viewport = Page.ViewportSize!;
            Assert.IsTrue(box!.X >= 0, "Dialog starts left of the viewport.");
            Assert.IsTrue(box.Y >= 0, "Dialog starts above the viewport.");
            Assert.IsTrue(box.X + box.Width <= viewport.Width, "Dialog extends past the right edge of the viewport.");
            Assert.IsTrue(box.Y + box.Height <= viewport.Height, "Dialog extends past the bottom edge of the viewport.");

            int overflow = await Page.EvaluateAsync<int>(
                "() => Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth)");
            Assert.AreEqual(0, overflow, "Unexpected horizontal page overflow at 1280x800.");
        }

        private async Task<ILocator> OpenExportDialogAsync()
        {
            await Page.Locator("button", new() { HasText = "Kundendaten exportieren" }).ClickAsync();
            ILocator dialog = Page.Locator(".export-customer-dialog-host");
            await Expect(dialog.Locator("dialog")).ToBeVisibleAsync();
            return dialog;
        }

        private async Task<ILocator> OpenImportDialogAsync()
        {
            await Page.Locator("button", new() { HasText = "Kundendaten importieren" }).ClickAsync();
            ILocator dialog = Page.Locator(".import-customer-dialog-host");
            await Expect(dialog.Locator("dialog")).ToBeVisibleAsync();
            return dialog;
        }

        private static string WriteTempFile(string fileName, string content)
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{fileName}");
            File.WriteAllText(path, content);
            return path;
        }
    }
}
