using System.Globalization;

using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Application.Features.Customers.Contracts;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Application.Features.Customers.ExportCustomerData;
using OrderManagement.Application.Features.Customers.ImportCustomerData;
using OrderManagement.Application.Features.Customers.SearchCustomers;

using Reqnroll;

using SharedKernel.Primitives;

namespace OrderManagement.AcceptanceTests.Steps
{
    [Binding]
    public sealed class CustomerDataExchangeSteps(
        ISearchCustomersUseCase searchCustomersUseCase,
        IImportCustomerDataUseCase importCustomerDataUseCase,
        IExportCustomerDataUseCase exportCustomerDataUseCase,
        ICustomerDataSerializerResolver serializerResolver)
    {
        private static readonly TimeZoneInfo ZurichTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        private CustomerDataFile? _fileToImport;
        private Result<ImportCustomerDataResponse>? _importResult;
        private Result<CustomerDataFile>? _exportResult;

        [Given(@"a JSON customer data file with:")]
        public async Task GivenAJsonCustomerDataFileWith(Table table) => await BuildFileAsync(CustomerDataFormat.Json, table);

        [Given(@"an XML customer data file with:")]
        public async Task GivenAnXmlCustomerDataFileWith(Table table) => await BuildFileAsync(CustomerDataFormat.Xml, table);

        [When(@"I import the customer data file")]
        public async Task WhenIImportTheCustomerDataFile() => _importResult = await importCustomerDataUseCase.ExecuteAsync(new ImportCustomerDataCommand(_fileToImport!));

        [When(@"I export the customer data as ""([^""]*)"" as of today")]
        public async Task WhenIExportTheCustomerDataAsAsOfToday(string formatName)
        {
            CustomerDataFormat format = Enum.Parse<CustomerDataFormat>(formatName, ignoreCase: true);
            DateTime zurichNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZurichTimeZone);

            _exportResult = await exportCustomerDataUseCase.ExecuteAsync(new ExportCustomerDataQuery(format, zurichNow.AddMinutes(10)));
        }

        [Then(@"the import succeeds with (\d+) imported customer(?:s)?")]
        public void ThenTheImportSucceedsWithImportedCustomers(int expectedCount)
        {
            Assert.IsTrue(_importResult!.Value.IsSuccess, _importResult.Value.Error);
            ImportCustomerDataResponse response = _importResult.Value.Value!;
            Assert.IsTrue(response.IsValid, string.Join("; ", response.Issues.Select(i => i.Message)));
            Assert.AreEqual(expectedCount, response.ImportedCount);
        }

        [Then(@"the import is rejected")]
        public void ThenTheImportIsRejected()
        {
            Assert.IsTrue(_importResult!.Value.IsSuccess, _importResult.Value.Error);
            Assert.IsFalse(_importResult.Value.Value!.IsValid);
        }

        [Then(@"customer ""([^""]*)"" exists with last name ""([^""]*)""")]
        public async Task ThenCustomerExistsWithLastName(string customerNumber, string expectedLastName)
        {
            CustomerListItemDto? match = await FindCustomerAsync(customerNumber);
            Assert.IsNotNull(match, $"Customer '{customerNumber}' was not found.");
            StringAssert.Contains(match.FullName, expectedLastName);
        }

        [Then(@"customer ""([^""]*)"" does not exist")]
        public async Task ThenCustomerDoesNotExist(string customerNumber)
        {
            CustomerListItemDto? match = await FindCustomerAsync(customerNumber);
            Assert.IsNull(match, $"Customer '{customerNumber}' was not expected to exist.");
        }

        [Then(@"the exported file contains customer ""([^""]*)"" with address ""([^""]*)""")]
        public async Task ThenTheExportedFileContainsCustomerWithAddress(string customerNumber, string expectedAddress)
        {
            Assert.IsTrue(_exportResult!.Value.IsSuccess, _exportResult.Value.Error);
            CustomerDataFile file = _exportResult.Value.Value!;

            Result<ICustomerDataSerializer> resolveResult = serializerResolver.Resolve(file.Format);
            Assert.IsTrue(resolveResult.IsSuccess, resolveResult.Error);

            using var stream = new MemoryStream(file.Content);
            Result<IReadOnlyList<CustomerDataDto>> deserializeResult = await resolveResult.Value!.DeserializeAsync(stream);
            Assert.IsTrue(deserializeResult.IsSuccess, deserializeResult.Error);

            CustomerDataDto match = deserializeResult.Value!.Single(c => c.CustomerNumber == customerNumber);
            Assert.IsNotNull(match.Address);
            Assert.AreEqual(expectedAddress, FormatAddress(match.Address!));
        }

        private async Task BuildFileAsync(CustomerDataFormat format, Table table)
        {
            List<CustomerDataDto> customers = [.. table.Rows.Select(row => new CustomerDataDto(
                row["CustomerNumber"],
                row["LastName"],
                row["SurName"],
                row["Email"],
                null,
                new CustomerAddressDataDto(
                    DateOnly.Parse(row["ValidFrom"], CultureInfo.InvariantCulture),
                    row["Street"],
                    row["HouseNumber"],
                    row["PostalCode"],
                    row["City"],
                    row["CountryCode"])))];

            Result<ICustomerDataSerializer> resolveResult = serializerResolver.Resolve(format);
            Assert.IsTrue(resolveResult.IsSuccess, resolveResult.Error);

            using var stream = new MemoryStream();
            await resolveResult.Value!.SerializeAsync(customers, stream);

            string extension = resolveResult.Value.FileExtension;
            _fileToImport = new CustomerDataFile($"kunden.{extension}", format, resolveResult.Value.MediaType, stream.ToArray());
        }

        private async Task<CustomerListItemDto?> FindCustomerAsync(string customerNumber)
        {
            Result<IReadOnlyList<CustomerListItemDto>> result = await searchCustomersUseCase.ExecuteAsync(new SearchCustomersQuery(customerNumber));
            Assert.IsTrue(result.IsSuccess, result.Error);
            return result.Value!.SingleOrDefault(c => c.CustomerNumber == customerNumber);
        }

        private static string FormatAddress(CustomerAddressDataDto address)
            => $"{address.Street} {address.HouseNumber}, {address.PostalCode} {address.City}, {address.CountryCode}";
    }
}
