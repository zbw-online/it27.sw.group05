using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Customers.AddCustomerAddress;
using OrderManagement.Application.Features.Customers.CreateCustomer;
using OrderManagement.Application.Features.Customers.DataExchange.Shared;
using OrderManagement.Application.Features.Customers.DeleteCustomer;
using OrderManagement.Application.Features.Customers.ExportCustomerData;
using OrderManagement.Application.Features.Customers.GetCustomerDetails;
using OrderManagement.Application.Features.Customers.GetCustomerForEdit;
using OrderManagement.Application.Features.Customers.ImportCustomerData;
using OrderManagement.Application.Features.Customers.SearchCustomers;
using OrderManagement.Application.Features.Customers.Shared;
using OrderManagement.Application.Features.Customers.UpdateCustomer;
using OrderManagement.Application.Features.Customers.ValidateCustomerDataImport;

using SharedKernel.Primitives;

using CustomersPage = OrderManagement.Presentation.Blazor.Components.Pages.Customers.Customers;

namespace OrderManagement.Presentation.Blazor.Tests.Customers
{
    [TestClass]
    public sealed class CustomersTests : Bunit.TestContext
    {
        private static readonly CustomerListItemDto[] Customers =
        [
            new(1, "CU00001", "Doe Jane", "jane@example.com", null, "Main Street", "1", "8000", "Zurich", "CH")
        ];

        public CustomersTests() => JSInterop.Mode = JSRuntimeMode.Loose;

        [TestMethod]
        public void ClickingCustomerRow_OpensDetailsDrawerWithCustomerInfo()
        {
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails());

            cut.Find("tbody tr").Click();

            string header = cut.Find(".customer-detail-header").TextContent;
            StringAssert.Contains(header, "Doe Jane");
            StringAssert.Contains(header, "CU00001");
            StringAssert.Contains(header, "jane@example.com");
        }

        [TestMethod]
        public void PressingEnterOnRow_OpensDetailsDrawer()
        {
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails());

            cut.Find("tbody tr").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.AreEqual(1, cut.FindAll(".customer-detail-header").Count);
        }

        [TestMethod]
        public void PressingSpaceOnRow_OpensDetailsDrawer()
        {
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails());

            cut.Find("tbody tr").KeyDown(new KeyboardEventArgs { Key = " " });

            Assert.AreEqual(1, cut.FindAll(".customer-detail-header").Count);
        }

        [TestMethod]
        public void DetailsDrawer_ShowsAddressSections_OrderedWithNextFutureAddressHighlighted()
        {
            var current = new CustomerAddressDto(1, new DateOnly(2026, 1, 1), null, "Current Street", "1", "9000", "St. Gallen", "CH", "Current");
            var nearFuture = new CustomerAddressDto(2, new DateOnly(2026, 9, 1), null, "Near Future Street", "2", "8000", "Zurich", "CH", "Future");
            var farFuture = new CustomerAddressDto(3, new DateOnly(2026, 12, 1), null, "Far Future Street", "3", "8001", "Zurich", "CH", "Future");
            var recentPrevious = new CustomerAddressDto(4, new DateOnly(2025, 6, 1), new DateOnly(2025, 12, 31), "Recent Previous Street", "4", "8002", "Zurich", "CH", "Previous");
            var olderPrevious = new CustomerAddressDto(5, new DateOnly(2024, 1, 1), new DateOnly(2025, 5, 31), "Older Previous Street", "5", "8003", "Zurich", "CH", "Previous");

            var details = new GetCustomerDetailsResponse(
                1, "CU00001", "Doe Jane", "jane@example.com", null,
                current,
                [recentPrevious, olderPrevious],
                [nearFuture, farFuture]);

            IRenderedComponent<CustomersPage> cut = RenderPage(details);

            cut.Find("tbody tr").Click();

            IElement[] sections = [.. cut.FindAll(".address-section")];
            Assert.AreEqual(3, sections.Length);

            StringAssert.Contains(sections[0].TextContent, "Aktuelle Adresse");
            StringAssert.Contains(sections[0].TextContent, "Current Street");

            StringAssert.Contains(sections[1].TextContent, "Zukünftige Adressen");
            int nearIndex = sections[1].TextContent.IndexOf("Near Future Street", StringComparison.Ordinal);
            int farIndex = sections[1].TextContent.IndexOf("Far Future Street", StringComparison.Ordinal);
            Assert.IsTrue(nearIndex >= 0 && farIndex > nearIndex);

            IElement[] futureCards = [.. sections[1].QuerySelectorAll(".address-card")];
            Assert.IsTrue(futureCards[0].ClassList.Contains("address-card-highlighted"));
            Assert.IsFalse(futureCards[1].ClassList.Contains("address-card-highlighted"));
            StringAssert.Contains(sections[1].TextContent, "Nächste geplante Adresse");

            StringAssert.Contains(sections[2].TextContent, "Frühere Adressen");
            int recentIndex = sections[2].TextContent.IndexOf("Recent Previous Street", StringComparison.Ordinal);
            int olderIndex = sections[2].TextContent.IndexOf("Older Previous Street", StringComparison.Ordinal);
            Assert.IsTrue(recentIndex >= 0 && olderIndex > recentIndex);
        }

        [TestMethod]
        public void DetailsDrawer_WithNoAddressesInASection_ShowsEmptyState()
        {
            var details = new GetCustomerDetailsResponse(
                1, "CU00001", "Doe Jane", "jane@example.com", null,
                null,
                [],
                []);

            IRenderedComponent<CustomersPage> cut = RenderPage(details);

            cut.Find("tbody tr").Click();

            Assert.AreEqual(3, cut.FindAll(".feedback-state-empty").Count);
        }

        [TestMethod]
        public void PageHeader_ShowsImportAndExportButtons()
        {
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails());

            IElement[] headerButtons = [.. cut.FindAll(".page-header-actions button")];

            Assert.IsTrue(headerButtons.Any(b => b.TextContent.Contains("Kundendaten importieren", StringComparison.Ordinal)));
            Assert.IsTrue(headerButtons.Any(b => b.TextContent.Contains("Kundendaten exportieren", StringComparison.Ordinal)));
        }

        [TestMethod]
        public void ClickingImportButton_OpensImportDialogWithSwissGermanLabels()
        {
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails());

            cut.Find(".page-header-actions button:nth-child(1)").Click();

            IElement dialog = cut.Find(".import-customer-dialog-host dialog[role=dialog]");
            StringAssert.Contains(dialog.TextContent, "Kundendaten importieren");
            StringAssert.Contains(dialog.TextContent, "Datei auswählen");
            StringAssert.Contains(dialog.TextContent, "In Datenbank importieren");
            StringAssert.Contains(dialog.TextContent, "Abbrechen");
        }

        [TestMethod]
        public void ClickingExportButton_OpensExportDialogWithSwissGermanLabels()
        {
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails());

            cut.Find(".page-header-actions button:nth-child(2)").Click();

            IElement dialog = cut.Find(".export-customer-dialog-host dialog[role=dialog]");
            StringAssert.Contains(dialog.TextContent, "Kundendaten exportieren");
            StringAssert.Contains(dialog.TextContent, "Stichtag");
            StringAssert.Contains(dialog.TextContent, "Exportieren");
            Assert.AreEqual("true", dialog.GetAttribute("aria-modal"));
        }

        [TestMethod]
        public void ImportDialog_WithUnsupportedFileExtension_ShowsErrorAsAlert()
        {
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails());
            cut.Find(".page-header-actions button:nth-child(1)").Click();

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "kunden.txt"));

            IElement alert = cut.Find(".inline-alert-danger");
            Assert.AreEqual("alert", alert.GetAttribute("role"));
            StringAssert.Contains(alert.TextContent, "Nicht unterstütztes Dateiformat");
        }

        [TestMethod]
        public void ImportDialog_BeforeCheckingFile_DisablesFinalImportButton()
        {
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails());
            cut.Find(".page-header-actions button:nth-child(1)").Click();

            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("[]", "kunden.json"));

            IElement importButton = cut.FindAll("button").Single(b => b.TextContent.Contains("In Datenbank importieren", StringComparison.Ordinal));
            Assert.IsTrue(importButton.HasAttribute("disabled"));
        }

        [TestMethod]
        public void ImportDialog_WithValidationIssues_KeepsFinalImportButtonDisabled()
        {
            var validateUseCase = new FakeValidateCustomerDataImportUseCase
            {
                ResponseToReturn = Results.Success(new ValidateCustomerDataImportResponse(
                    false, 2, [new CustomerImportValidationIssue(1, "CU00002", "email", "Die E-Mail-Adresse ist ungültig.")])),
            };
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails(), validateUseCase: validateUseCase);
            cut.Find(".page-header-actions button:nth-child(1)").Click();
            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("[]", "kunden.json"));

            cut.FindAll("button").Single(b => b.TextContent.Contains("Datei prüfen", StringComparison.Ordinal)).Click();

            StringAssert.Contains(cut.Markup, "Die E-Mail-Adresse ist ungültig.");
            IElement importButton = cut.FindAll("button").Single(b => b.TextContent.Contains("In Datenbank importieren", StringComparison.Ordinal));
            Assert.IsTrue(importButton.HasAttribute("disabled"));
        }

        [TestMethod]
        public void ImportDialog_WithSuccessfulImport_ShowsSummaryOnParentPageAndClosesDialog()
        {
            var validateUseCase = new FakeValidateCustomerDataImportUseCase
            {
                ResponseToReturn = Results.Success(new ValidateCustomerDataImportResponse(true, 3, [])),
            };
            var importUseCase = new FakeImportCustomerDataUseCase
            {
                ResponseToReturn = Results.Success(new ImportCustomerDataResponse(true, 3, 3, [])),
            };
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails(), validateUseCase: validateUseCase, importUseCase: importUseCase);
            cut.Find(".page-header-actions button:nth-child(1)").Click();
            cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("[]", "kunden.json"));
            cut.FindAll("button").Single(b => b.TextContent.Contains("Datei prüfen", StringComparison.Ordinal)).Click();

            cut.FindAll("button").Single(b => b.TextContent.Contains("In Datenbank importieren", StringComparison.Ordinal)).Click();

            StringAssert.Contains(cut.Markup, "3 Kunden wurden importiert.");
        }

        [TestMethod]
        public void ExportDialog_WithXmlFormatSelected_PassesXmlFormatToUseCase()
        {
            var exportUseCase = new FakeExportCustomerDataUseCase();
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails(), exportUseCase: exportUseCase);
            cut.Find(".page-header-actions button:nth-child(2)").Click();

            cut.Find("input[value=Xml]").Change(true);
            cut.FindAll("button").Single(b => b.TextContent.Contains("Exportieren", StringComparison.Ordinal)).Click();

            Assert.IsNotNull(exportUseCase.CapturedQuery);
            Assert.AreEqual(CustomerDataFormat.Xml, exportUseCase.CapturedQuery!.Format);
        }

        [TestMethod]
        public void ExportDialog_WithChosenStichtag_PassesStichtagToUseCase()
        {
            var exportUseCase = new FakeExportCustomerDataUseCase();
            IRenderedComponent<CustomersPage> cut = RenderPage(BuildDetails(), exportUseCase: exportUseCase);
            cut.Find(".page-header-actions button:nth-child(2)").Click();

            cut.Find("#export-stichtag").Change("2026-01-15T18:30:00");
            cut.FindAll("button").Single(b => b.TextContent.Contains("Exportieren", StringComparison.Ordinal)).Click();

            Assert.IsNotNull(exportUseCase.CapturedQuery);
            Assert.AreEqual(new DateTime(2026, 1, 15, 18, 30, 0), exportUseCase.CapturedQuery!.Stichtag);
        }

        private static GetCustomerDetailsResponse BuildDetails() => new(
            1, "CU00001", "Doe Jane", "jane@example.com", null, null, [], []);

        private IRenderedComponent<CustomersPage> RenderPage(
            GetCustomerDetailsResponse details,
            FakeValidateCustomerDataImportUseCase? validateUseCase = null,
            FakeImportCustomerDataUseCase? importUseCase = null,
            FakeExportCustomerDataUseCase? exportUseCase = null)
        {
            _ = Services.AddSingleton<ISearchCustomersUseCase>(new FakeSearchCustomersUseCase());
            _ = Services.AddSingleton<IGetCustomerDetailsUseCase>(new FakeGetCustomerDetailsUseCase(details));
            _ = Services.AddSingleton<ICreateCustomerUseCase>(new FakeCreateCustomerUseCase());
            _ = Services.AddSingleton<IGetCustomerForEditUseCase>(new FakeGetCustomerForEditUseCase());
            _ = Services.AddSingleton<IUpdateCustomerUseCase>(new FakeUpdateCustomerUseCase());
            _ = Services.AddSingleton<IDeleteCustomerUseCase>(new FakeDeleteCustomerUseCase());
            _ = Services.AddSingleton<IAddCustomerAddressUseCase>(new FakeAddCustomerAddressUseCase());
            _ = Services.AddSingleton<IValidateCustomerDataImportUseCase>(validateUseCase ?? new FakeValidateCustomerDataImportUseCase());
            _ = Services.AddSingleton<IImportCustomerDataUseCase>(importUseCase ?? new FakeImportCustomerDataUseCase());
            _ = Services.AddSingleton<IExportCustomerDataUseCase>(exportUseCase ?? new FakeExportCustomerDataUseCase());
            _ = Services.AddSingleton(Options.Create(new CustomerDataExchangeOptions()));
            _ = Services.AddSingleton(TimeProvider.System);

            return RenderComponent<CustomersPage>();
        }

        private sealed class FakeSearchCustomersUseCase : ISearchCustomersUseCase
        {
            public Task<Result<IReadOnlyList<CustomerListItemDto>>> ExecuteAsync(
                SearchCustomersQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Success<IReadOnlyList<CustomerListItemDto>>(Customers));
        }

        private sealed class FakeGetCustomerDetailsUseCase(GetCustomerDetailsResponse response) : IGetCustomerDetailsUseCase
        {
            public Task<Result<GetCustomerDetailsResponse>> ExecuteAsync(
                GetCustomerDetailsQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Success(response));
        }

        private sealed class FakeCreateCustomerUseCase : ICreateCustomerUseCase
        {
            public Task<Result<CreateCustomerResponse>> ExecuteAsync(
                CreateCustomerCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Fail<CreateCustomerResponse>("not used"));
        }

        private sealed class FakeGetCustomerForEditUseCase : IGetCustomerForEditUseCase
        {
            public Task<Result<GetCustomerForEditResponse>> ExecuteAsync(
                GetCustomerForEditQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Fail<GetCustomerForEditResponse>("not used"));
        }

        private sealed class FakeUpdateCustomerUseCase : IUpdateCustomerUseCase
        {
            public Task<Result> ExecuteAsync(
                UpdateCustomerCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeDeleteCustomerUseCase : IDeleteCustomerUseCase
        {
            public Task<Result> ExecuteAsync(
                DeleteCustomerCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeAddCustomerAddressUseCase : IAddCustomerAddressUseCase
        {
            public Task<Result> ExecuteAsync(
                AddCustomerAddressCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeValidateCustomerDataImportUseCase : IValidateCustomerDataImportUseCase
        {
            public Result<ValidateCustomerDataImportResponse> ResponseToReturn { get; set; } =
                Results.Success(new ValidateCustomerDataImportResponse(true, 0, []));

            public Task<Result<ValidateCustomerDataImportResponse>> ExecuteAsync(
                ValidateCustomerDataImportQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(ResponseToReturn);
        }

        private sealed class FakeImportCustomerDataUseCase : IImportCustomerDataUseCase
        {
            public Result<ImportCustomerDataResponse> ResponseToReturn { get; set; } =
                Results.Success(new ImportCustomerDataResponse(true, 0, 0, []));

            public Task<Result<ImportCustomerDataResponse>> ExecuteAsync(
                ImportCustomerDataCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(ResponseToReturn);
        }

        private sealed class FakeExportCustomerDataUseCase : IExportCustomerDataUseCase
        {
            public ExportCustomerDataQuery? CapturedQuery { get; private set; }

            public Result<CustomerDataFile> ResponseToReturn { get; set; } =
                Results.Success(new CustomerDataFile("kunden.json", CustomerDataFormat.Json, "application/json", [1, 2, 3]));

            public Task<Result<CustomerDataFile>> ExecuteAsync(
                ExportCustomerDataQuery query, CancellationToken cancellationToken = default)
            {
                CapturedQuery = query;
                return Task.FromResult(ResponseToReturn);
            }
        }
    }
}
