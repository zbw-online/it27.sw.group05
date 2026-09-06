using Microsoft.Extensions.Options;

using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Application.Features.Customers.ImportCustomerData;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Application.Tests.Fakes.Customers.DataExchange;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers.DataExchange
{
    [TestClass]
    public sealed class ImportCustomerDataUseCaseTests
    {
        private static CustomerDataDto ValidCustomer(string customerNumber = "CU00001", string email = "hans.muster@example.ch")
            => new(customerNumber, "Muster", "Hans", email, "www.example.ch",
                new CustomerAddressDataDto(new DateOnly(2026, 1, 1), "Musterstrasse", "10", "8000", "Zürich", "CH"));

        private static CustomerDataFile MakeFile(CustomerDataFormat format = CustomerDataFormat.Json)
            => new("kunden.json", format, "application/json", [1, 2, 3]);

        private static IOptions<CustomerDataExchangeOptions> DefaultOptions()
            => Options.Create(new CustomerDataExchangeOptions());

        [TestMethod]
        public async Task ExecuteAsync_WithSingleValidCustomer_ShouldAddAndCommitOnce()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>([ValidCustomer()]),
            };
            var commandRepository = new FakeCustomerCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var planBuilder = new CustomerImportPlanBuilder(
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeCustomerQueryRepository(),
                DefaultOptions());
            var useCase = new ImportCustomerDataUseCase(planBuilder, commandRepository, unitOfWork);

            Result<ImportCustomerDataResponse> result = await useCase.ExecuteAsync(new ImportCustomerDataCommand(MakeFile()));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(result.Value!.IsValid);
            Assert.AreEqual(1, result.Value.ImportedCount);
            Assert.AreEqual(1, commandRepository.Added.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMultipleValidCustomers_ShouldAddAllAndCommitOnce()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer("CU00001", "a@b.ch"), ValidCustomer("CU00002", "c@d.ch"), ValidCustomer("CU00003", "e@f.ch")]),
            };
            var commandRepository = new FakeCustomerCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var planBuilder = new CustomerImportPlanBuilder(
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeCustomerQueryRepository(),
                DefaultOptions());
            var useCase = new ImportCustomerDataUseCase(planBuilder, commandRepository, unitOfWork);

            Result<ImportCustomerDataResponse> result = await useCase.ExecuteAsync(new ImportCustomerDataCommand(MakeFile()));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(3, result.Value!.ImportedCount);
            Assert.AreEqual(3, commandRepository.Added.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithOneInvalidRecord_ShouldAddNoneAndNotCommit()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>(
                    [ValidCustomer("CU00001", "a@b.ch"), ValidCustomer("not-valid", "c@d.ch")]),
            };
            var commandRepository = new FakeCustomerCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var planBuilder = new CustomerImportPlanBuilder(
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeCustomerQueryRepository(),
                DefaultOptions());
            var useCase = new ImportCustomerDataUseCase(planBuilder, commandRepository, unitOfWork);

            Result<ImportCustomerDataResponse> result = await useCase.ExecuteAsync(new ImportCustomerDataCommand(MakeFile()));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsFalse(result.Value!.IsValid);
            Assert.AreEqual(0, result.Value.ImportedCount);
            Assert.AreEqual(1, result.Value.Issues.Count);
            Assert.AreEqual(0, commandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WhenCommitFails_ShouldReturnFailure()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json)
            {
                DeserializeResult = Results.Success<IReadOnlyList<CustomerDataDto>>([ValidCustomer()]),
            };
            var commandRepository = new FakeCustomerCommandRepository();
            var unitOfWork = new FakeUnitOfWork { FailureMessage = "Database unavailable." };
            var planBuilder = new CustomerImportPlanBuilder(
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeCustomerQueryRepository(),
                DefaultOptions());
            var useCase = new ImportCustomerDataUseCase(planBuilder, commandRepository, unitOfWork);

            Result<ImportCustomerDataResponse> result = await useCase.ExecuteAsync(new ImportCustomerDataCommand(MakeFile()));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Database unavailable.", result.Error);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCancelledToken_ShouldThrow()
        {
            var planBuilder = new CustomerImportPlanBuilder(
                new FakeCustomerDataSerializerResolver(),
                new FakeCustomerQueryRepository(),
                DefaultOptions());
            var useCase = new ImportCustomerDataUseCase(planBuilder, new FakeCustomerCommandRepository(), new FakeUnitOfWork());
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => useCase.ExecuteAsync(new ImportCustomerDataCommand(MakeFile()), cts.Token));
        }
    }
}
