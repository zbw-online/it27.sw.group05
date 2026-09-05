using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Customers.DataExchange.Shared;
using OrderManagement.Application.Features.Customers.ValidateCustomerDataImport;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Application.Tests.Fakes.Customers.DataExchange;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers.DataExchange
{
    [TestClass]
    public sealed class ValidateCustomerDataImportUseCaseTests
    {
        private static CustomerDataDto ValidCustomer(string customerNumber = "CU00001", string email = "hans.muster@example.ch")
            => new(customerNumber, "Muster", "Hans", email, "www.example.ch",
                new CustomerAddressDataDto(new DateOnly(2026, 1, 1), "Musterstrasse", "10", "8000", "Zürich", "CH"));

        private static CustomerDataFile MakeFile(CustomerDataFormat format = CustomerDataFormat.Json)
            => new("kunden.json", format, "application/json", [1, 2, 3]);

        private static IOptions<CustomerDataExchangeOptions> DefaultOptions()
            => Options.Create(new CustomerDataExchangeOptions());

        [TestMethod]
        public async Task ExecuteAsync_WithValidFile_ShouldReturnValidPreview()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>([ValidCustomer()]),
            };
            var planBuilder = new CustomerImportPlanBuilder(
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeCustomerQueryRepository(),
                DefaultOptions());
            var useCase = new ValidateCustomerDataImportUseCase(planBuilder);

            Result<ValidateCustomerDataImportResponse> result = await useCase.ExecuteAsync(new ValidateCustomerDataImportQuery(MakeFile()));

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Value!.IsValid);
            Assert.AreEqual(1, result.Value.TotalRecordCount);
            Assert.AreEqual(0, result.Value.Issues.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithInvalidCustomer_ShouldReturnIssuesButStillSucceed()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>([ValidCustomer(customerNumber: "invalid")]),
            };
            var planBuilder = new CustomerImportPlanBuilder(
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeCustomerQueryRepository(),
                DefaultOptions());
            var useCase = new ValidateCustomerDataImportUseCase(planBuilder);

            Result<ValidateCustomerDataImportResponse> result = await useCase.ExecuteAsync(new ValidateCustomerDataImportQuery(MakeFile()));

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.Value!.IsValid);
            Assert.AreEqual(1, result.Value.Issues.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnsupportedFormat_ShouldReturnFileLevelIssue()
        {
            var planBuilder = new CustomerImportPlanBuilder(
                new FakeCustomerDataSerializerResolver(),
                new FakeCustomerQueryRepository(),
                DefaultOptions());
            var useCase = new ValidateCustomerDataImportUseCase(planBuilder);

            Result<ValidateCustomerDataImportResponse> result = await useCase.ExecuteAsync(new ValidateCustomerDataImportQuery(MakeFile()));

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(result.Value!.IsValid);
            Assert.AreEqual("file", result.Value.Issues[0].Field);
        }
    }
}
