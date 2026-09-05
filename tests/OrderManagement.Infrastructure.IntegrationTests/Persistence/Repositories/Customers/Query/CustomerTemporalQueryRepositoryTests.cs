using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Customers.DataExchange.Shared;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Customers.Query
{
    [TestClass]
    public sealed class CustomerTemporalQueryRepositoryTests : IntegrationTestBase
    {
        private static readonly string[] ExpectedOrderedCustomerNumbers = ["CU50101", "CU50102", "CU50103"];

        [TestMethod]
        public async Task GetCustomersAsOfAsync_WithCustomerChangedAfterStichtag_ShouldReturnOldValues()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext, customerNumber: "CU50001", lastName: "Original", surName: "Name");

            // Use the original row's own persisted period start as the as-of point: it is exactly the
            // boundary of the pre-change version, so it deterministically selects that version
            // regardless of the later update's timing (no race against an independently-read clock).
            DateTime stichtagUtc = await DbContext.Database.SqlQueryRaw<DateTime>(
                "SELECT [RowValidFrom] AS [Value] FROM [Customers] WHERE [CustomerNumber] = 'CU50001'")
                .SingleAsync();

            Result changeResult = customer.ChangeName("Changed", "Name");
            Assert.IsTrue(changeResult.IsSuccess, changeResult.Error);
            _ = await DbContext.SaveChangesAsync();

            var repository = new CustomerTemporalQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetCustomersAsOfAsync(stichtagUtc, DateOnly.FromDateTime(stichtagUtc));

            CustomerDataDto dto = result.Single(c => c.CustomerNumber == "CU50001");
            Assert.AreEqual("Original", dto.LastName);
        }

        [TestMethod]
        public async Task GetCustomersAsOfAsync_WithAddressActiveOnBusinessDate_ShouldReturnThatAddress()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                customerNumber: "CU50002",
                validFrom: new DateOnly(2026, 1, 1),
                street: "Erste Strasse");

            var repository = new CustomerTemporalQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetCustomersAsOfAsync(
                DateTime.UtcNow, new DateOnly(2026, 6, 1));

            CustomerDataDto dto = result.Single(c => c.CustomerNumber == "CU50002");
            Assert.IsNotNull(dto.Address);
            Assert.AreEqual("Erste Strasse", dto.Address!.Street);
        }

        [TestMethod]
        public async Task GetCustomersAsOfAsync_WithAddressClosedBeforeBusinessDate_ShouldReturnLaterAddress()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                customerNumber: "CU50003",
                validFrom: new DateOnly(2026, 1, 1),
                street: "Erste Strasse");

            Result changeResult = customer.ChangeAddress(
                new DateOnly(2026, 7, 1), "Zweite Strasse", "2", "8001", "Zürich", "CH");
            Assert.IsTrue(changeResult.IsSuccess, changeResult.Error);
            _ = await DbContext.SaveChangesAsync();

            var repository = new CustomerTemporalQueryRepository(DbContext);

            IReadOnlyList<CustomerDataDto> beforeSwitch = await repository.GetCustomersAsOfAsync(
                DateTime.UtcNow, new DateOnly(2026, 6, 1));
            IReadOnlyList<CustomerDataDto> afterSwitch = await repository.GetCustomersAsOfAsync(
                DateTime.UtcNow, new DateOnly(2026, 8, 1));

            Assert.AreEqual("Erste Strasse", beforeSwitch.Single(c => c.CustomerNumber == "CU50003").Address!.Street);
            Assert.AreEqual("Zweite Strasse", afterSwitch.Single(c => c.CustomerNumber == "CU50003").Address!.Street);
        }

        [TestMethod]
        public async Task GetCustomersAsOfAsync_WithFutureAddress_ShouldExcludeIt()
        {
            Result<Customer> createResult = Customer.Create("CU50004", "Future", "Address", "future.address@test.local", null);
            Assert.IsTrue(createResult.IsSuccess, createResult.Error);
            Customer customer = createResult.EnsureValue();

            Result addressResult = customer.ChangeAddress(
                new DateOnly(2027, 1, 1), "Zukunftsstrasse", "1", "8000", "Zürich", "CH");
            Assert.IsTrue(addressResult.IsSuccess, addressResult.Error);

            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            var repository = new CustomerTemporalQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetCustomersAsOfAsync(
                DateTime.UtcNow, new DateOnly(2026, 6, 1));

            Assert.IsNull(result.Single(c => c.CustomerNumber == "CU50004").Address);
        }

        [TestMethod]
        public async Task GetCustomersAsOfAsync_WithoutAnyAddress_ShouldReturnNullAddress()
        {
            Result<Customer> createResult = Customer.Create("CU50005", "No", "Address", "no.address@test.local", null);
            Assert.IsTrue(createResult.IsSuccess, createResult.Error);
            Customer customer = createResult.EnsureValue();

            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            var repository = new CustomerTemporalQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetCustomersAsOfAsync(
                DateTime.UtcNow, DateOnly.FromDateTime(DateTime.UtcNow));

            Assert.IsNull(result.Single(c => c.CustomerNumber == "CU50005").Address);
        }

        [TestMethod]
        public async Task GetCustomersAsOfAsync_WithCustomerDeletedAfterStichtag_ShouldStillIncludeCustomer()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                customerNumber: "CU50006",
                lastName: "Deleted",
                surName: "Later",
                validFrom: new DateOnly(2026, 1, 1),
                street: "Alte Strasse");

            // Use the row's own persisted period start as the as-of point (see the "changed after
            // Stichtag" test above for why this avoids racing an independently-read clock).
            DateTime stichtagUtc = await DbContext.Database.SqlQueryRaw<DateTime>(
                "SELECT [RowValidFrom] AS [Value] FROM [Customers] WHERE [CustomerNumber] = 'CU50006'")
                .SingleAsync();

            _ = DbContext.Customers.Remove(customer);
            _ = await DbContext.SaveChangesAsync();

            var repository = new CustomerTemporalQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetCustomersAsOfAsync(
                stichtagUtc, new DateOnly(2026, 6, 1));

            CustomerDataDto dto = result.Single(c => c.CustomerNumber == "CU50006");
            Assert.AreEqual("Deleted", dto.LastName);
            Assert.IsNotNull(dto.Address);
            Assert.AreEqual("Alte Strasse", dto.Address!.Street);
        }

        [TestMethod]
        public async Task GetCustomersAsOfAsync_WithCustomerCreatedAfterStichtag_ShouldExcludeCustomer()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU50007");

            // Derive the "before creation" instant from the row's own persisted system-versioning
            // period start rather than an independently-captured clock reading, so the boundary is
            // exact by construction instead of racing two separate round trips to the server.
            DateTime rowValidFrom = await DbContext.Database.SqlQueryRaw<DateTime>(
                "SELECT [RowValidFrom] AS [Value] FROM [Customers] WHERE [CustomerNumber] = 'CU50007'")
                .SingleAsync();
            DateTime stichtagUtc = rowValidFrom.AddTicks(-1);

            var repository = new CustomerTemporalQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetCustomersAsOfAsync(
                stichtagUtc, DateOnly.FromDateTime(stichtagUtc));

            Assert.IsFalse(result.Any(c => c.CustomerNumber == "CU50007"));
        }

        [TestMethod]
        public async Task GetCustomersAsOfAsync_ShouldOrderByCustomerNumber()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU50103");
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU50101");
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU50102");

            var repository = new CustomerTemporalQueryRepository(DbContext);
            IReadOnlyList<CustomerDataDto> result = await repository.GetCustomersAsOfAsync(
                DateTime.UtcNow, DateOnly.FromDateTime(DateTime.UtcNow));

            var ourNumbers = result
                .Select(c => c.CustomerNumber)
                .Where(n => n is "CU50101" or "CU50102" or "CU50103")
                .ToList();

            CollectionAssert.AreEqual(ExpectedOrderedCustomerNumbers, ourNumbers);
        }
    }
}
