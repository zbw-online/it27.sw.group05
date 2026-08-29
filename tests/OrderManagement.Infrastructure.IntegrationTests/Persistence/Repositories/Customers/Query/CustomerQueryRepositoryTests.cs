using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Customers.Query
{
    [TestClass]
    public sealed class CustomerQueryRepositoryTests : IntegrationTestBase
    {
        private CustomerQueryRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new CustomerQueryRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task GetByIdAsync_WithExistingCustomer_ShouldReturnCustomerIncludingAddressesAsNoTracking()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            DbContext.ChangeTracker.Clear();

            Customer? result = await _repository.GetByIdAsync(customer.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(customer.Id, result.Id);
            Assert.AreEqual(1, result.Addresses.Count);
            Assert.IsFalse(DbContext.ChangeTracker.Entries<Customer>().Any());
        }

        [TestMethod]
        public async Task GetByCustomerNumberAsync_WithExistingNumber_ShouldReturnCustomer()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                customerNumber: "CU21001");

            CustomerNumber number = CustomerNumber.Create("CU21001").EnsureValue();
            DbContext.ChangeTracker.Clear();

            Customer? result = await _repository.GetByCustomerNumberAsync(number);

            Assert.IsNotNull(result);
            Assert.AreEqual(customer.Id, result.Id);
        }

        [TestMethod]
        public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnCustomer()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                email: "email.match@test.local");

            Email email = Email.Create("email.match@test.local").EnsureValue();
            DbContext.ChangeTracker.Clear();

            Customer? result = await _repository.GetByEmailAsync(email);

            Assert.IsNotNull(result);
            Assert.AreEqual(customer.Id, result.Id);
        }

        [TestMethod]
        public async Task SearchByNameAsync_WithMatchingTerm_ShouldReturnMatchingCustomersOnly()
        {
            Customer matching = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                lastName: "Schneider",
                surName: "Anna");

            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                lastName: "Meier",
                surName: "Peter");

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Customer> result = await _repository.SearchByNameOrNumberAsync("Schneid");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(matching.Id, result.Single().Id);
        }

        [TestMethod]
        public async Task GetListAsync_WithCustomers_ShouldReturnAllCustomers()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            _ = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Customer> result = await _repository.GetListAsync();

            Assert.AreEqual(2, result.Count);
        }
    }
}
