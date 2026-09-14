using OrderManagement.Application.Features.Invoices.Contracts;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Persistence.Repositories.Invoices.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Invoices.Query
{
    [TestClass]
    public sealed class InvoiceQueryRepositoryTests : IntegrationTestBase
    {
        private InvoiceQueryRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new InvoiceQueryRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task GetOrdersWithHistoricalAddressAsync_ShouldReturnAddressValidAtOrderDate()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                customerNumber: "CU23001",
                lastName: "Arpanet",
                surName: "AG",
                validFrom: new DateOnly(2017, 1, 1),
                street: "Old Street",
                houseNumber: "1",
                postalCode: "8000",
                city: "Zurich");

            Order order1 = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(
                DbContext,
                customer.Id,
                orderNumber: "ORD-2017-001",
                orderDate: new DateTime(2017, 3, 31, 12, 0, 0));

            Result addressChange = customer.ChangeAddress(
                validFrom: new DateOnly(2017, 4, 2),
                street: "New Street",
                houseNumber: "2",
                postalCode: "9000",
                city: "St. Gallen",
                countryCode: "CH");

            Assert.IsTrue(addressChange.IsSuccess, addressChange.Error);
            _ = await DbContext.SaveChangesAsync();

            Order order2 = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(
                DbContext,
                customer.Id,
                orderNumber: "ORD-2017-002",
                orderDate: new DateTime(2017, 4, 30, 12, 0, 0));

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<InvoiceDto> result = await _repository.GetOrdersWithHistoricalAddressAsync(
                fromDate: new DateTime(2017, 1, 1),
                toDate: new DateTime(2017, 12, 31));

            InvoiceDto firstInvoice = result.Single(x => x.Rechnungsnummer == order1.OrderNumber.Value);
            InvoiceDto secondInvoice = result.Single(x => x.Rechnungsnummer == order2.OrderNumber.Value);

            Assert.AreEqual("Old Street 1", firstInvoice.Strasse);
            Assert.AreEqual("8000", firstInvoice.PLZ);
            Assert.AreEqual("Zurich", firstInvoice.Ort);

            Assert.AreEqual("New Street 2", secondInvoice.Strasse);
            Assert.AreEqual("9000", secondInvoice.PLZ);
            Assert.AreEqual("St. Gallen", secondInvoice.Ort);
        }

        [TestMethod]
        public async Task GetOrdersWithHistoricalAddressAsync_WithCustomerNumberFilter_ShouldReturnOnlyMatchingCustomer()
        {
            Customer customer1 = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU23002");
            Customer customer2 = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU23003");

            _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, customer1.Id, "ORD-2024-001");
            _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, customer2.Id, "ORD-2024-002");

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<InvoiceDto> result = await _repository.GetOrdersWithHistoricalAddressAsync(
                customerNumber: "CU23002");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("CU23002", result.Single().Kundennummer);
            Assert.AreEqual("ORD-2024-001", result.Single().Rechnungsnummer);
        }
    }
}
