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
        private CustomerCommandRepository? _repository;

        [TestInitialize]
        public void Setup()
        {
            Assert.IsNotNull(DbContext);
            _repository = new CustomerCommandRepository(DbContext);
        }

        [TestMethod]
        public async Task Add_ShouldPersistCustomerWithAddress()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<Customer> createResult = Customer.Create(
                id: 10_001,
                customerNr: "C-10001",
                lastName: "Doe",
                surName: "John",
                email: "john.doe@tests.local",
                website: "https://example.com",
                passwordHash: "hash"
            );

            Assert.IsTrue(createResult.IsSuccess);
            Customer customer = createResult.Value!;

            Result addrResult = customer.ChangeAddress(
                validFrom: new DateOnly(2026, 01, 01),
                street: "Main Street",
                houseNumber: "1A",
                postalCode: "8000",
                city: "Zurich",
                countryCode: "CH"
            );
            Assert.IsTrue(addrResult.IsSuccess);

            _repository.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            // Re-load from DB to verify mapping + collection persistence
            Customer? retrieved = await DbContext.Customers
                .Include(c => c.Addresses)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == new CustomerId(10_001));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("C-10001", retrieved.CustomerNumber.Value);
            Assert.AreEqual("Doe", retrieved.LastName);
            Assert.AreEqual("John", retrieved.SurName);
            Assert.AreEqual("john.doe@tests.local", retrieved.Email.Value);
            Assert.AreEqual("https://example.com", retrieved.Website);
            Assert.AreEqual("hash", retrieved.PasswordHash);

            Assert.AreEqual(1, retrieved.Addresses.Count);
            CustomerAddress address = retrieved.Addresses.Single();
            Assert.AreEqual(new DateOnly(2026, 01, 01), address.ValidFrom);
            Assert.IsNull(address.ValidTo);
            Assert.AreEqual("Main Street", address.Street);
            Assert.AreEqual("1A", address.HouseNumber);
            Assert.AreEqual("8000", address.PostalCode);
            Assert.AreEqual("Zurich", address.City);
            Assert.AreEqual("CH", address.CountryCode);
        }

        [TestMethod]
        public async Task Update_ShouldModifyWebsiteAndPasswordHash()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<Customer> createResult = Customer.Create(
                id: 10_002,
                customerNr: "C-10002",
                lastName: "Miller",
                surName: "Alice",
                email: "alice.miller@tests.local",
                website: null,
                passwordHash: "hash1"
            );

            Assert.IsTrue(createResult.IsSuccess);
            Customer customer = createResult.Value!;

            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            // Detach, then fetch tracked instance to update
            DbContext.Entry(customer).State = EntityState.Detached;

            Customer? tracked = await DbContext.Customers
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == new CustomerId(10_002));

            Assert.IsNotNull(tracked);

            Result websiteRes = tracked.ChangeWebsite("https://changed.example");
            Assert.IsTrue(websiteRes.IsSuccess);

            Result passRes = tracked.SetPasswordHash("hash2");
            Assert.IsTrue(passRes.IsSuccess);

            _repository.Update(tracked);
            _ = await DbContext.SaveChangesAsync();

            Customer? updated = await DbContext.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == new CustomerId(10_002));

            Assert.IsNotNull(updated);
            Assert.AreEqual("https://changed.example", updated.Website);
            Assert.AreEqual("hash2", updated.PasswordHash);
        }

        [TestMethod]
        public async Task Remove_ShouldDeleteCustomer()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<Customer> createResult = Customer.Create(
                id: 10_003,
                customerNr: "C-10003",
                lastName: "Delete",
                surName: "Me",
                email: "delete.me@tests.local",
                website: null,
                passwordHash: "hash"
            );

            Assert.IsTrue(createResult.IsSuccess);
            Customer customer = createResult.Value!;

            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            _repository.Remove(customer);
            _ = await DbContext.SaveChangesAsync();

            Customer? deleted = await DbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == new CustomerId(10_003));

            Assert.IsNull(deleted);
        }
    }
}
