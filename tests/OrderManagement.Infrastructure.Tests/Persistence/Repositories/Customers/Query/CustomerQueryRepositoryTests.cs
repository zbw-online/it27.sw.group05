using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Customers.Query
{
    [TestClass]
    public sealed class CustomerQueryRepositoryTests : IntegrationTestBase
    {
        private CustomerQueryRepository? _repository;

        [TestInitialize]
        public void Setup()
        {
            Assert.IsNotNull(DbContext);
            _repository = new CustomerQueryRepository(DbContext);
        }

        [TestMethod]
        public async Task GetByIdAsync_ExistingCustomer_ReturnsCustomerIncludingAddresses()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<Customer> createResult = Customer.Create(
                id: 20_001,
                customerNr: "C-20001",
                lastName: "Doe",
                surName: "Jane",
                email: "jane.doe@tests.local",
                website: null,
                passwordHash: "hash"
            );

            Assert.IsTrue(createResult.IsSuccess);
            Customer customer = createResult.Value!;

            _ = customer.ChangeAddress(new DateOnly(2026, 01, 01), "Street", "1", "8000", "Zurich", "CH");
            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            Customer? retrieved = await _repository.GetByIdAsync(new CustomerId(20_001));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("C-20001", retrieved.CustomerNumber.Value);
            Assert.AreEqual(1, retrieved.Addresses.Count);
        }

        [TestMethod]
        public async Task GetListAsync_ReturnsAllCustomers()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            string suffix = Guid.NewGuid().ToString("N")[..8];

            int[] ids = [30010, 30011, 30012, 30013, 30014];

            foreach (int id in ids)
            {
                Result<Customer> res = Customer.Create(
                    id: id,
                    customerNr: $"C-{id % 10000:00000}",
                    lastName: "Last",
                    surName: $"S{id}",
                    email: $"user{id}.{suffix}@tests.local",
                    website: null,
                    passwordHash: "hash"
                );

                Assert.IsTrue(res.IsSuccess, res.Error);
                _ = DbContext.Customers.Add(res.Value!);
            }

            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<Customer> customers = await _repository.GetListAsync();

            // Don’t assert exact count if DB is shared across tests
            foreach (int id in ids)
                Assert.IsTrue(customers.Any(c => c.Id.Value == id));
        }

        [TestMethod]
        public async Task GetByCustomerNumberAsync_ExistingNumber_ReturnsCustomer()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<Customer> createResult = Customer.Create(
                id: 20_020,
                customerNr: "C-20020",
                lastName: "Number",
                surName: "Match",
                email: "number.match@tests.local",
                website: null,
                passwordHash: "hash"
            );

            Assert.IsTrue(createResult.IsSuccess);
            _ = DbContext.Customers.Add(createResult.Value!);
            _ = await DbContext.SaveChangesAsync();

            Result<CustomerNumber> number = CustomerNumber.Create("C-20020");
            Assert.IsTrue(number.IsSuccess);

            Customer? retrieved = await _repository.GetByCustomerNumberAsync(number.Value!);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("C-20020", retrieved.CustomerNumber.Value);
        }

        [TestMethod]
        public async Task GetByEmailAsync_ExistingEmail_ReturnsCustomer()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<Customer> createResult = Customer.Create(
                id: 20_030,
                customerNr: "C-20030",
                lastName: "Email",
                surName: "Match",
                email: "email.match@tests.local",
                website: null,
                passwordHash: "hash"
            );

            Assert.IsTrue(createResult.IsSuccess);
            _ = DbContext.Customers.Add(createResult.Value!);
            _ = await DbContext.SaveChangesAsync();

            Result<Email> email = Email.Create("email.match@tests.local");
            Assert.IsTrue(email.IsSuccess);

            Customer? retrieved = await _repository.GetByEmailAsync(email.Value!);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("email.match@tests.local", retrieved.Email.Value);
        }

        [TestMethod]
        public async Task SearchByNameAsync_ReturnsMatchesOnLastNameOrSurName()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<Customer> c1 = Customer.Create(
                id: 20_040,
                customerNr: "C-20040",
                lastName: "Anderson",
                surName: "Bob",
                email: "bob.anderson@tests.local",
                website: null,
                passwordHash: "hash"
            );
            Assert.IsTrue(c1.IsSuccess);

            Result<Customer> c2 = Customer.Create(
                id: 20_041,
                customerNr: "C-20041",
                lastName: "Smith",
                surName: "Anders",
                email: "anders.smith@tests.local",
                website: null,
                passwordHash: "hash"
            );
            Assert.IsTrue(c2.IsSuccess);

            DbContext.Customers.AddRange(c1.Value!, c2.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<Customer> results = await _repository.SearchByNameAsync("Ander");

            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public async Task AddressAt_ReturnsCorrectAddressForGivenDate()
        {
            // This is domain behavior, but running it through persistence verifies
            // Address collection mapping, DateOnly mapping, and ordering.
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<Customer> createResult = Customer.Create(
                id: 20_050,
                customerNr: "C-20050",
                lastName: "Temporal",
                surName: "Address",
                email: "temporal@tests.local",
                website: null,
                passwordHash: "hash"
            );

            Assert.IsTrue(createResult.IsSuccess);
            Customer customer = createResult.Value!;

            Result a1 = customer.ChangeAddress(new DateOnly(2026, 01, 01), "Old", "1", "8000", "Zurich", "CH");
            Assert.IsTrue(a1.IsSuccess);

            Result a2 = customer.ChangeAddress(new DateOnly(2026, 02, 01), "New", "2", "8001", "Zurich", "CH");
            Assert.IsTrue(a2.IsSuccess);

            _ = DbContext.Customers.Add(customer);
            _ = await DbContext.SaveChangesAsync();

            Customer? retrieved = await _repository.GetByIdAsync(new CustomerId(20_050));
            Assert.IsNotNull(retrieved);

            CustomerAddress? jan = retrieved.AddressAt(new DateOnly(2026, 01, 15));
            Assert.IsNotNull(jan);
            Assert.AreEqual("Old", jan.Street);

            CustomerAddress? feb = retrieved.AddressAt(new DateOnly(2026, 02, 15));
            Assert.IsNotNull(feb);
            Assert.AreEqual("New", feb.Street);
        }
    }
}
