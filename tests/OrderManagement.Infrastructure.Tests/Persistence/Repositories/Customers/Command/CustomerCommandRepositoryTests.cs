using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Customers.Command
{
    [TestClass]
    public sealed class CustomerCommandRepositoryTests : IntegrationTestBase
    {
        private CustomerCommandRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new CustomerCommandRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task Add_WithValidCustomer_ShouldPersistCustomerAndAddressAndGenerateId()
        {
            Customer customer = Customer.Create(
                customerNr: "CU20001",
                lastName: "Muster",
                surName: "Hans",
                email: "hans.muster@test.local",
                website: null).EnsureValue();

            Result addressResult = customer.ChangeAddress(
                validFrom: new DateOnly(2026, 1, 1),
                street: "Bahnhofstrasse",
                houseNumber: "10",
                postalCode: "9000",
                city: "St. Gallen",
                countryCode: "CH");

            Assert.IsTrue(addressResult.IsSuccess, addressResult.Error);

            _repository.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            CustomerId customerId = customer.Id;
            Assert.IsTrue(customerId.IsAssigned);

            DbContext.ChangeTracker.Clear();

            Customer? persisted = await DbContext.Customers
                .Include(c => c.Addresses)
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == customerId);

            Assert.IsNotNull(persisted);
            Assert.AreEqual("CU20001", persisted.CustomerNumber.Value);
            Assert.AreEqual("Muster", persisted.LastName);
            Assert.AreEqual("Hans", persisted.SurName);
            Assert.AreEqual(1, persisted.Addresses.Count);
        }

        [TestMethod]
        public async Task Update_WithChangedWebsite_ShouldPersistChange()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            CustomerId customerId = customer.Id;

            DbContext.ChangeTracker.Clear();

            Customer tracked = await DbContext.Customers.SingleAsync(c => c.Id == customerId);
            Result result = tracked.ChangeWebsite("https://example.ch");
            Assert.IsTrue(result.IsSuccess, result.Error);

            _repository.Update(tracked);
            _ = await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();

            Customer? persisted = await DbContext.Customers
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == customerId);

            Assert.IsNotNull(persisted);
            Assert.AreEqual("https://example.ch", persisted.Website);
        }

        [TestMethod]
        public async Task Remove_WithExistingCustomer_ShouldDeleteCustomerAndCascadeAddresses()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            CustomerId customerId = customer.Id;

            DbContext.ChangeTracker.Clear();

            Customer tracked = await DbContext.Customers
                .Include(c => c.Addresses)
                .SingleAsync(c => c.Id == customerId);

            int addressId = tracked.Addresses.Single().Id;

            _repository.Remove(tracked);
            _ = await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();

            bool customerExists = await DbContext.Customers.AsNoTracking().AnyAsync(c => c.Id == customerId);
            bool addressExists = await DbContext.CustomerAddresses.AsNoTracking().AnyAsync(a => a.Id == addressId);

            Assert.IsFalse(customerExists);
            Assert.IsFalse(addressExists);
        }
    }
}
