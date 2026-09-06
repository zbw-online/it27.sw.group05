using Microsoft.Extensions.Options;

using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Application.Features.Customers.ImportCustomerData;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;
using OrderManagement.Infrastructure.Serialization.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Application.Customers
{
    [TestClass]
    public sealed class ImportCustomerDataUseCaseIntegrationTests : IntegrationTestBase
    {
        private CustomerCommandRepository _commandRepository = default!;
        private CustomerQueryRepository _queryRepository = default!;
        private UnitOfWork _unitOfWork = default!;
        private JsonCustomerDataSerializer _jsonSerializer = default!;
        private CustomerImportPlanBuilder _planBuilder = default!;
        private ImportCustomerDataUseCase _useCase = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _commandRepository = new CustomerCommandRepository(DbContext);
            _queryRepository = new CustomerQueryRepository(DbContext);
            _unitOfWork = new UnitOfWork(DbContext);

            IOptions<CustomerDataExchangeOptions> options = Options.Create(new CustomerDataExchangeOptions());
            _jsonSerializer = new JsonCustomerDataSerializer(options);
            var resolver = new CustomerDataSerializerResolver([_jsonSerializer, new XmlCustomerDataSerializer(options)]);
            _planBuilder = new CustomerImportPlanBuilder(resolver, _queryRepository, options);
            _useCase = new ImportCustomerDataUseCase(_planBuilder, _commandRepository, _unitOfWork);

            return Task.CompletedTask;
        }

        private static CustomerDataDto ValidCustomer(string customerNumber, string email)
            => new(customerNumber, "Muster", "Hans", email, "www.example.ch",
                new CustomerAddressDataDto(new DateOnly(2026, 1, 1), "Musterstrasse", "10", "8000", "Zürich", "CH"));

        private async Task<CustomerDataFile> BuildJsonFileAsync(params CustomerDataDto[] customers)
        {
            using var stream = new MemoryStream();
            await _jsonSerializer.SerializeAsync(customers, stream);
            return new CustomerDataFile("kunden.json", CustomerDataFormat.Json, "application/json", stream.ToArray());
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMultipleValidCustomers_ShouldPersistAllAndCommitOnce()
        {
            CustomerDataFile file = await BuildJsonFileAsync(
                ValidCustomer("CU60001", "import1@test.local"),
                ValidCustomer("CU60002", "import2@test.local"),
                ValidCustomer("CU60003", "import3@test.local"));

            Result<ImportCustomerDataResponse> result = await _useCase.ExecuteAsync(new ImportCustomerDataCommand(file));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(result.Value!.IsValid);
            Assert.AreEqual(3, result.Value.ImportedCount);

            DbContext.ChangeTracker.Clear();
            Assert.IsNotNull(await _queryRepository.GetByCustomerNumberAsync(CustomerNumber.Create("CU60001").EnsureValue()));
            Assert.IsNotNull(await _queryRepository.GetByCustomerNumberAsync(CustomerNumber.Create("CU60002").EnsureValue()));
            Assert.IsNotNull(await _queryRepository.GetByCustomerNumberAsync(CustomerNumber.Create("CU60003").EnsureValue()));
        }

        [TestMethod]
        public async Task ExecuteAsync_WithOneInvalidRecord_ShouldPersistNone()
        {
            CustomerDataFile file = await BuildJsonFileAsync(
                ValidCustomer("CU60011", "import11@test.local"),
                ValidCustomer("not-valid-number", "import12@test.local"));

            Result<ImportCustomerDataResponse> result = await _useCase.ExecuteAsync(new ImportCustomerDataCommand(file));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsFalse(result.Value!.IsValid);
            Assert.AreEqual(0, result.Value.ImportedCount);

            DbContext.ChangeTracker.Clear();
            Customer? persisted = await _queryRepository.GetByCustomerNumberAsync(CustomerNumber.Create("CU60011").EnsureValue());

            Assert.IsNull(persisted, "Atomic import must not persist any customer when one record is invalid.");
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCustomerNumberAlreadyInDatabase_ShouldRejectAndPersistNothing()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU60021");
            DbContext.ChangeTracker.Clear();

            CustomerDataFile file = await BuildJsonFileAsync(
                ValidCustomer("CU60021", "conflict-number@test.local"),
                ValidCustomer("CU60022", "import22@test.local"));

            Result<ImportCustomerDataResponse> result = await _useCase.ExecuteAsync(new ImportCustomerDataCommand(file));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsFalse(result.Value!.IsValid);

            DbContext.ChangeTracker.Clear();
            Customer? otherPersisted = await _queryRepository.GetByCustomerNumberAsync(CustomerNumber.Create("CU60022").EnsureValue());

            Assert.IsNull(otherPersisted, "Atomic import must reject the whole batch on a database conflict.");
        }

        [TestMethod]
        public async Task ExecuteAsync_WithEmailAlreadyInDatabase_ShouldRejectAndPersistNothing()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext, customerNumber: "CU60031", email: "existing-email@test.local");
            DbContext.ChangeTracker.Clear();

            CustomerDataFile file = await BuildJsonFileAsync(
                ValidCustomer("CU60032", "existing-email@test.local"),
                ValidCustomer("CU60033", "import33@test.local"));

            Result<ImportCustomerDataResponse> result = await _useCase.ExecuteAsync(new ImportCustomerDataCommand(file));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsFalse(result.Value!.IsValid);

            DbContext.ChangeTracker.Clear();
            Customer? otherPersisted = await _queryRepository.GetByCustomerNumberAsync(CustomerNumber.Create("CU60033").EnsureValue());

            Assert.IsNull(otherPersisted, "Atomic import must reject the whole batch on an email conflict.");
        }
    }
}
