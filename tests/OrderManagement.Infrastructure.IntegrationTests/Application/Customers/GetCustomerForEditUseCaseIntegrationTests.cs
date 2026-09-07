using OrderManagement.Application.Features.Customers.GetCustomerForEdit;
using OrderManagement.Application.Features.Customers.UpdateCustomer;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Application.Customers
{
    [TestClass]
    public sealed class GetCustomerForEditUseCaseIntegrationTests : IntegrationTestBase
    {
        private static readonly DateOnly Today = new(2026, 9, 7);

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
        public async Task ExecuteAsync_WithCurrentAddress_ReturnsMasterDataAndCurrentAddress()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                lastName: "Muster",
                surName: "Maria",
                validFrom: Today.AddMonths(-1),
                street: "Alte Gasse");
            DbContext.ChangeTracker.Clear();

            var useCase = new GetCustomerForEditUseCase(_queryRepository, new FakeTimeProvider(Today));

            Result<GetCustomerForEditResponse> result = await useCase.ExecuteAsync(new GetCustomerForEditQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            GetCustomerForEditResponse response = result.EnsureValue();
            Assert.AreEqual("Muster", response.LastName);
            Assert.AreEqual("Maria", response.SurName);
            Assert.AreEqual("Alte Gasse", response.Street);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCurrentAndFutureAddress_SelectsAddressValidOnTimeProviderToday()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                validFrom: Today.AddMonths(-6),
                street: "Alte Gasse");

            var addressCommandRepository = new CustomerCommandRepository(DbContext);
            Customer tracked = (await addressCommandRepository.GetByIdAsync(customer.Id))!;
            tracked.ChangeAddress(Today.AddDays(1), "Neue Gasse", "2", "8000", "Zürich", "CH").EnsureSuccess();
            addressCommandRepository.Update(tracked);
            _ = await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();

            var useCase = new GetCustomerForEditUseCase(_queryRepository, new FakeTimeProvider(Today));

            Result<GetCustomerForEditResponse> result = await useCase.ExecuteAsync(new GetCustomerForEditQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Alte Gasse", result.EnsureValue().Street);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithOnlyFutureAddress_FallsBackToThatAddress()
        {
            Customer customer = Customer.Create(
                InfrastructureTestDataFactory.NextCustomerNumber(), "Zukunft", "Fritz",
                $"future-{Guid.NewGuid():N}@test.local", null).EnsureValue();
            customer.ChangeAddress(Today.AddMonths(1), "Neue Gasse", "2", "8000", "Zürich", "CH").EnsureSuccess();
            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();

            var useCase = new GetCustomerForEditUseCase(_queryRepository, new FakeTimeProvider(Today));

            Result<GetCustomerForEditResponse> result = await useCase.ExecuteAsync(new GetCustomerForEditQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Neue Gasse", result.EnsureValue().Street);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithNoAddress_ReturnsEmptyAddressFields()
        {
            Customer customer = Customer.Create(
                InfrastructureTestDataFactory.NextCustomerNumber(), "Ohne", "Adresse",
                $"noaddress-{Guid.NewGuid():N}@test.local", null).EnsureValue();
            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();

            var useCase = new GetCustomerForEditUseCase(_queryRepository, new FakeTimeProvider(Today));

            Result<GetCustomerForEditResponse> result = await useCase.ExecuteAsync(new GetCustomerForEditQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            GetCustomerForEditResponse response = result.EnsureValue();
            Assert.AreEqual(string.Empty, response.Street);
            Assert.AreEqual(string.Empty, response.HouseNumber);
            Assert.AreEqual(string.Empty, response.PostalCode);
            Assert.AreEqual(string.Empty, response.City);
            Assert.AreEqual("CH", response.CountryCode);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownCustomerId_Fails()
        {
            var useCase = new GetCustomerForEditUseCase(_queryRepository, new FakeTimeProvider(Today));

            Result<GetCustomerForEditResponse> result = await useCase.ExecuteAsync(new GetCustomerForEditQuery(999_999));

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithAlreadyCanceledToken_ThrowsOperationCanceledException()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            var useCase = new GetCustomerForEditUseCase(_queryRepository, new FakeTimeProvider(Today));

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(
                () => useCase.ExecuteAsync(new GetCustomerForEditQuery(customer.Id.Value), cts.Token));
        }

        [TestMethod]
        public async Task EditThenUpdate_RoundTrip_PersistsChangesAndReflectsThemOnReload()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                lastName: "Vorher",
                surName: "Name",
                validFrom: Today.AddMonths(-1),
                street: "Alte Gasse");
            DbContext.ChangeTracker.Clear();

            var editUseCase = new GetCustomerForEditUseCase(_queryRepository, new FakeTimeProvider(Today));
            Result<GetCustomerForEditResponse> editResult = await editUseCase.ExecuteAsync(new GetCustomerForEditQuery(customer.Id.Value));
            Assert.IsTrue(editResult.IsSuccess, editResult.Error);
            GetCustomerForEditResponse loaded = editResult.EnsureValue();

            var updateUseCase = new UpdateCustomerUseCase(_commandRepository, _queryRepository, _unitOfWork, new FakeTimeProvider(Today));
            Result updateResult = await updateUseCase.ExecuteAsync(new UpdateCustomerCommand(
                loaded.CustomerId, "Nachher", "Name", loaded.Email, loaded.Website,
                Today, "Neue Gasse", loaded.HouseNumber, loaded.PostalCode, loaded.City, loaded.CountryCode));
            Assert.IsTrue(updateResult.IsSuccess, updateResult.Error);

            DbContext.ChangeTracker.Clear();

            Result<GetCustomerForEditResponse> reloadResult = await editUseCase.ExecuteAsync(new GetCustomerForEditQuery(customer.Id.Value));
            Assert.IsTrue(reloadResult.IsSuccess, reloadResult.Error);
            GetCustomerForEditResponse reloaded = reloadResult.EnsureValue();
            Assert.AreEqual("Nachher", reloaded.LastName);
            Assert.AreEqual("Neue Gasse", reloaded.Street);
        }

        [TestMethod]
        public async Task Commit_WhenUnrelatedTrackedChangeViolatesUniqueConstraint_DoesNotPersistOtherChangesInSameUnitOfWork()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, lastName: "Original");
            DbContext.ChangeTracker.Clear();

            Customer tracked = (await _commandRepository.GetByIdAsync(customer.Id))!;
            tracked.ChangeName("Geaendert", "Geaendert").EnsureSuccess();
            _commandRepository.Update(tracked);

            Customer duplicateNumberCustomer = Customer.Create(
                customer.CustomerNumber.Value, "Duplikat", "Kunde",
                $"duplicate-{Guid.NewGuid():N}@test.local", null).EnsureValue();
            duplicateNumberCustomer.ChangeAddress(Today, "X-Strasse", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            _commandRepository.Add(duplicateNumberCustomer);

            Result commitResult = await _unitOfWork.CommitAsync();
            Assert.IsFalse(commitResult.IsSuccess, "A duplicate CustomerNumber must violate the unique index and fail the commit.");

            DbContext.ChangeTracker.Clear();
            Customer? reloaded = await _commandRepository.GetByIdAsync(customer.Id);
            Assert.AreEqual(
                "Original",
                reloaded!.LastName,
                "A failed commit must not persist unrelated tracked changes from the same unit of work.");
        }

        private sealed class FakeTimeProvider(DateOnly today) : TimeProvider
        {
            private readonly DateTimeOffset _now = new(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => _now;

            public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        }
    }
}
