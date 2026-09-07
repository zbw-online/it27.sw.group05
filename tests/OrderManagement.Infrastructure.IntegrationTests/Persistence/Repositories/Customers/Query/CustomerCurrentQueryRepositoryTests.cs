using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Customers.Query
{
    [TestClass]
    public sealed class CustomerCurrentQueryRepositoryTests : IntegrationTestBase
    {
        private static readonly string[] ExpectedOrderedCustomerNumbers = ["CU51101", "CU51102", "CU51103"];

        [TestMethod]
        public async Task GetAllCurrentAsync_ImmediatelyAfterCustomerCreation_ShouldIncludeTheCustomer()
        {
            // Regression test for the empty-export bug: unlike the temporal repository, the current
            // repository never uses TemporalAsOf, so it cannot be affected by a gap between the
            // TimeProvider-supplied "now" and SQL Server's SysStartTime for a freshly inserted row.
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU51001");

            var repository = new CustomerCurrentQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetAllCurrentAsync(DateOnly.FromDateTime(DateTime.UtcNow));

            Assert.IsTrue(result.Any(c => c.CustomerNumber == "CU51001"));
        }

        [TestMethod]
        public async Task GetAllCurrentAsync_WithAddressActiveToday_ShouldReturnThatAddress()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                customerNumber: "CU51002",
                validFrom: new DateOnly(2026, 1, 1),
                street: "Erste Strasse");

            var repository = new CustomerCurrentQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetAllCurrentAsync(new DateOnly(2026, 6, 1));

            CustomerDataDto dto = result.Single(c => c.CustomerNumber == "CU51002");
            Assert.IsNotNull(dto.Address);
            Assert.AreEqual("Erste Strasse", dto.Address!.Street);
        }

        [TestMethod]
        public async Task GetAllCurrentAsync_WithAddressChangedSinceEarlierBusinessDate_ShouldReturnLatestAddress()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                customerNumber: "CU51003",
                validFrom: new DateOnly(2026, 1, 1),
                street: "Erste Strasse");

            Result changeResult = customer.ChangeAddress(
                new DateOnly(2026, 7, 1), "Zweite Strasse", "2", "8001", "Zürich", "CH");
            Assert.IsTrue(changeResult.IsSuccess, changeResult.Error);
            _ = await DbContext.SaveChangesAsync();

            var repository = new CustomerCurrentQueryRepository(DbContext);

            IReadOnlyList<CustomerDataDto> beforeSwitch = await repository.GetAllCurrentAsync(new DateOnly(2026, 6, 1));
            IReadOnlyList<CustomerDataDto> afterSwitch = await repository.GetAllCurrentAsync(new DateOnly(2026, 8, 1));

            Assert.AreEqual("Erste Strasse", beforeSwitch.Single(c => c.CustomerNumber == "CU51003").Address!.Street);
            Assert.AreEqual("Zweite Strasse", afterSwitch.Single(c => c.CustomerNumber == "CU51003").Address!.Street);
        }

        [TestMethod]
        public async Task GetAllCurrentAsync_WithFutureAddress_ShouldExcludeIt()
        {
            Result<Customer> createResult = Customer.Create("CU51004", "Future", "Address", "future.address.current@test.local", null);
            Assert.IsTrue(createResult.IsSuccess, createResult.Error);
            Customer customer = createResult.EnsureValue();

            Result addressResult = customer.ChangeAddress(
                new DateOnly(2027, 1, 1), "Zukunftsstrasse", "1", "8000", "Zürich", "CH");
            Assert.IsTrue(addressResult.IsSuccess, addressResult.Error);

            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            var repository = new CustomerCurrentQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetAllCurrentAsync(new DateOnly(2026, 6, 1));

            Assert.IsNull(result.Single(c => c.CustomerNumber == "CU51004").Address);
        }

        [TestMethod]
        public async Task GetAllCurrentAsync_WithoutAnyAddress_ShouldReturnNullAddressButIncludeCustomer()
        {
            Result<Customer> createResult = Customer.Create("CU51005", "No", "Address", "no.address.current@test.local", null);
            Assert.IsTrue(createResult.IsSuccess, createResult.Error);
            Customer customer = createResult.EnsureValue();

            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            var repository = new CustomerCurrentQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetAllCurrentAsync(DateOnly.FromDateTime(DateTime.UtcNow));

            CustomerDataDto dto = result.Single(c => c.CustomerNumber == "CU51005");
            Assert.IsNull(dto.Address);
        }

        [TestMethod]
        public async Task GetAllCurrentAsync_ShouldReturnEachCustomerExactlyOnce()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU51006");

            var repository = new CustomerCurrentQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetAllCurrentAsync(DateOnly.FromDateTime(DateTime.UtcNow));

            Assert.AreEqual(1, result.Count(c => c.CustomerNumber == "CU51006"));
        }

        [TestMethod]
        public async Task GetAllCurrentAsync_ShouldOrderByCustomerNumber()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU51103");
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU51101");
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU51102");

            var repository = new CustomerCurrentQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetAllCurrentAsync(DateOnly.FromDateTime(DateTime.UtcNow));

            var ourNumbers = result
                .Select(c => c.CustomerNumber)
                .Where(n => n is "CU51101" or "CU51102" or "CU51103")
                .ToList();

            CollectionAssert.AreEqual(ExpectedOrderedCustomerNumbers, ourNumbers);
        }
    }
}
