using Microsoft.Extensions.Options;

using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Application.Tests.Fakes.Customers.DataExchange;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers.DataExchange
{
    [TestClass]
    public sealed class CustomerImportPlanBuilderTests
    {
        private static IOptions<CustomerDataExchangeOptions> DefaultOptions()
            => Options.Create(new CustomerDataExchangeOptions());

        private static CustomerAddressDataDto ValidAddress()
            => new(new DateOnly(2026, 1, 1), "Musterstrasse", "10", "8000", "Zürich", "CH");

        private static CustomerDataDto ValidCustomer(
            string customerNumber = "CU00001",
            string email = "hans.muster@example.ch",
            CustomerAddressDataDto? address = null)
            => new(customerNumber, "Muster", "Hans", email, "www.example.ch", address ?? ValidAddress());

        private static CustomerDataFile MakeFile(CustomerDataFormat format = CustomerDataFormat.Json)
            => new("kunden.json", format, "application/json", [1, 2, 3]);

        [TestMethod]
        public async Task BuildAsync_WithUnsupportedFormat_ShouldReturnFileLevelIssue()
        {
            var resolver = new FakeCustomerDataSerializerResolver();
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual(1, plan.Issues.Count);
            Assert.AreEqual("file", plan.Issues[0].Field);
        }

        [TestMethod]
        public async Task BuildAsync_WithValidSingleCustomer_ShouldProduceValidPlan()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>([ValidCustomer()]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsTrue(plan.IsValid, string.Join("; ", plan.Issues.Select(i => i.Message)));
            Assert.AreEqual(1, plan.TotalRecordCount);
            Assert.AreEqual(1, plan.CustomersToImport.Count);
            Assert.AreEqual("CU00001", plan.CustomersToImport[0].CustomerNumber.Value);
            Assert.AreEqual(1, plan.CustomersToImport[0].Addresses.Count);
        }

        [TestMethod]
        public async Task BuildAsync_WithNullAddress_ShouldProduceCustomerWithoutAddress()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer(address: null) with { Address = null }]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsTrue(plan.IsValid, string.Join("; ", plan.Issues.Select(i => i.Message)));
            Assert.AreEqual(0, plan.CustomersToImport[0].Addresses.Count);
        }

        [TestMethod]
        public async Task BuildAsync_WithInvalidCustomerNumber_ShouldReturnIssueOnCustomerNumberField()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer(customerNumber: "not-a-number")]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual(0, plan.Issues[0].RecordIndex);
            Assert.AreEqual("customerNumber", plan.Issues[0].Field);
            Assert.AreEqual(0, plan.CustomersToImport.Count);
        }

        [TestMethod]
        public async Task BuildAsync_WithInvalidEmail_ShouldReturnIssueOnEmailField()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer(email: "not-an-email")]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual("email", plan.Issues[0].Field);
        }

        [TestMethod]
        public async Task BuildAsync_WithInvalidWebsite_ShouldReturnIssue()
        {
            CustomerDataDto customer = ValidCustomer() with { Website = "not a website" };
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>([customer]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
        }

        [TestMethod]
        public async Task BuildAsync_WithBlankLastName_ShouldReturnIssue()
        {
            CustomerDataDto customer = ValidCustomer() with { LastName = "   " };
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>([customer]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
        }

        [TestMethod]
        public async Task BuildAsync_WithInvalidAddress_ShouldReturnIssueOnAddressField()
        {
            CustomerAddressDataDto invalidAddress = ValidAddress() with { Street = "" };
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer(address: invalidAddress)]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual("address", plan.Issues[0].Field);
        }

        [TestMethod]
        public async Task BuildAsync_WithDuplicateCustomerNumberInFile_ShouldReturnIssueOnSecondOccurrence()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer(email: "a@b.ch"), ValidCustomer(email: "c@d.ch")]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual(1, plan.Issues.Count);
            Assert.AreEqual(1, plan.Issues[0].RecordIndex);
            Assert.AreEqual("customerNumber", plan.Issues[0].Field);
        }

        [TestMethod]
        public async Task BuildAsync_WithDuplicateEmailInFile_ShouldReturnIssueOnSecondOccurrence()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer("CU00001"), ValidCustomer("CU00002")]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual(1, plan.Issues.Count);
            Assert.AreEqual("email", plan.Issues[0].Field);
        }

        [TestMethod]
        public async Task BuildAsync_WithCaseInsensitiveDuplicateEmailInFile_ShouldReturnIssue()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer("CU00001", email: "Hans@Example.CH"), ValidCustomer("CU00002", email: "hans@example.ch")]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual("email", plan.Issues[0].Field);
        }

        [TestMethod]
        public async Task BuildAsync_WithCustomerNumberConflictInDatabase_ShouldReturnIssue()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>([ValidCustomer("CU00001")]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            _ = queryRepository.Seed(Customer.Create("CU00001", "Existing", "Customer", "existing@example.com", null).EnsureValue());
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual("customerNumber", plan.Issues[0].Field);
            Assert.AreEqual(0, plan.CustomersToImport.Count);
        }

        [TestMethod]
        public async Task BuildAsync_WithEmailConflictInDatabase_ShouldReturnIssue()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer("CU00002", email: "hans.muster@example.ch")]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            _ = queryRepository.Seed(Customer.Create("CU00099", "Existing", "Customer", "hans.muster@example.ch", null).EnsureValue());
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual("email", plan.Issues[0].Field);
        }

        [TestMethod]
        public async Task BuildAsync_WithMultipleValidCustomers_ShouldNotQueryDatabasePerCustomer()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer("CU00001", "a@b.ch"), ValidCustomer("CU00002", "c@d.ch"), ValidCustomer("CU00003", "e@f.ch")]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsTrue(plan.IsValid, string.Join("; ", plan.Issues.Select(i => i.Message)));
            Assert.AreEqual(3, plan.CustomersToImport.Count);
            Assert.AreEqual(1, queryRepository.GetListCallCount);
        }

        [TestMethod]
        public async Task BuildAsync_WithFileExceedingConfiguredMaxSize_ShouldReturnFileLevelIssue()
        {
            var resolver = new FakeCustomerDataSerializerResolver();
            var queryRepository = new FakeCustomerQueryRepository();
            IOptions<CustomerDataExchangeOptions> smallLimit = Options.Create(new CustomerDataExchangeOptions { MaxFileSizeBytes = 2 });
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, smallLimit);

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual("file", plan.Issues[0].Field);
        }

        [TestMethod]
        public async Task BuildAsync_WithRecordCountExceedingConfiguredMax_ShouldReturnFileLevelIssue()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer("CU00001", "a@b.ch"), ValidCustomer("CU00002", "c@d.ch")]),
            };
            var resolver = new FakeCustomerDataSerializerResolver(jsonSerializer);
            var queryRepository = new FakeCustomerQueryRepository();
            IOptions<CustomerDataExchangeOptions> smallLimit = Options.Create(new CustomerDataExchangeOptions { MaxCustomerCount = 1 });
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, smallLimit);

            CustomerImportPlan plan = await builder.BuildAsync(MakeFile());

            Assert.IsFalse(plan.IsValid);
            Assert.AreEqual(2, plan.TotalRecordCount);
            StringAssert.Contains(plan.Issues[0].Message, "1");
        }

        [TestMethod]
        public async Task BuildAsync_WithCancelledToken_ShouldThrow()
        {
            var resolver = new FakeCustomerDataSerializerResolver();
            var queryRepository = new FakeCustomerQueryRepository();
            var builder = new CustomerImportPlanBuilder(resolver, queryRepository, DefaultOptions());
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => builder.BuildAsync(MakeFile(), cts.Token));
        }
    }
}
