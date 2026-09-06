using OrderManagement.Application.Features.Customers.AddCustomerAddress;
using OrderManagement.Application.Features.Customers.GetCustomerDetails;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Application.Customers
{
    [TestClass]
    public sealed class CustomerAddressUseCaseTests : IntegrationTestBase
    {
        private CustomerCommandRepository _commandRepository = default!;
        private CustomerQueryRepository _queryRepository = default!;
        private UnitOfWork _unitOfWork = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _commandRepository = new CustomerCommandRepository(DbContext);
            _queryRepository = new CustomerQueryRepository(DbContext);
            _unitOfWork = new UnitOfWork(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task AddCustomerAddressUseCase_WithFutureAddress_ShouldPersistAndClassifyAddressAsFuture()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                validFrom: DateOnly.FromDateTime(DateTime.Today).AddMonths(-1),
                street: "Current Street");

            var addUseCase = new AddCustomerAddressUseCase(_commandRepository, _unitOfWork);
            var detailsUseCase = new GetCustomerDetailsUseCase(_queryRepository, TimeProvider.System);

            DateOnly futureValidFrom = DateOnly.FromDateTime(DateTime.Today).AddMonths(1);

            Result addResult = await addUseCase.ExecuteAsync(new AddCustomerAddressCommand(
                customer.Id.Value,
                futureValidFrom,
                "Future Street",
                "22",
                "8000",
                "Zurich",
                "CH"));

            Assert.IsTrue(addResult.IsSuccess, addResult.Error);

            DbContext.ChangeTracker.Clear();

            Result<GetCustomerDetailsResponse> detailsResult = await detailsUseCase.ExecuteAsync(
                new GetCustomerDetailsQuery(customer.Id.Value));

            Assert.IsTrue(detailsResult.IsSuccess, detailsResult.Error);
            GetCustomerDetailsResponse details = detailsResult.EnsureValue();

            Assert.IsNotNull(details.CurrentAddress);
            Assert.AreEqual("Current Street", details.CurrentAddress.Street);
            Assert.AreEqual(1, details.FutureAddresses.Count);
            Assert.AreEqual("Future Street", details.FutureAddresses.Single().Street);
            Assert.AreEqual(futureValidFrom, details.FutureAddresses.Single().ValidFrom);
        }

        [TestMethod]
        public async Task GetCustomerDetailsUseCase_WithPreviousCurrentAndFutureAddresses_ShouldReturnSeparatedAddressGroups()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly previousValidFrom = today.AddMonths(-3);
            DateOnly currentValidFrom = today.AddMonths(-1);
            DateOnly futureValidFrom = today.AddMonths(1);

            Customer customer = Customer.Create(
                customerNr: InfrastructureTestDataFactory.NextCustomerNumber(),
                lastName: "Address",
                surName: "Tester",
                email: $"address-tester-{Guid.NewGuid():N}@test.local",
                website: "example.com").EnsureValue();

            customer.ChangeAddress(previousValidFrom, "Previous Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(currentValidFrom, "Current Street", "2", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(futureValidFrom, "Future Street", "3", "8000", "Zurich", "CH").EnsureSuccess();

            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();

            var detailsUseCase = new GetCustomerDetailsUseCase(_queryRepository, TimeProvider.System);

            Result<GetCustomerDetailsResponse> result = await detailsUseCase.ExecuteAsync(
                new GetCustomerDetailsQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            GetCustomerDetailsResponse details = result.EnsureValue();

            Assert.IsNotNull(details.CurrentAddress);
            Assert.AreEqual("Current Street", details.CurrentAddress.Street);
            Assert.AreEqual(1, details.PreviousAddresses.Count);
            Assert.AreEqual("Previous Street", details.PreviousAddresses.Single().Street);
            Assert.AreEqual(1, details.FutureAddresses.Count);
            Assert.AreEqual("Future Street", details.FutureAddresses.Single().Street);
        }
    }
}
