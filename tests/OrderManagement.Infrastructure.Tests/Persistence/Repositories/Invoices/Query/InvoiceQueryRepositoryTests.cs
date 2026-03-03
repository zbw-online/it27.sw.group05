using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.DTOs.Invoices;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Persistence.Repositories.Invoices.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Invoices.Query
{
    [TestClass]
    public sealed class OrderInvoiceQueryRepositoryTests : IntegrationTestBase
    {
        private InvoiceQueryRepository _sut = default!;

        [TestInitialize]
        public void Setup() => _sut = new InvoiceQueryRepository(DbContext!);

        [TestMethod]
        public async Task GetOrdersWithHistoricalAddress_ReturnsCorrectAddressAtOrderDate()
        {
            // Arrange - Kunde mit initialer Adresse
            Result<Customer> customer = Customer.Create(
                1234,
                "C-01234",
                "Müller",
                "Hans",
                "hans@arpanet.ch",
                null,
                "hash123");

            if (!customer.IsSuccess) Assert.Fail(customer.Error ?? "Customer creation failed");

            // Erste Adresse hinzufügen
            Result addOldAddress = customer.EnsureValue().ChangeAddress(
                DateOnly.FromDateTime(new DateTime(2017, 1, 1)),
                "Alte Strasse",
                "10",
                "8000",
                "Zürich",
                "CH");

            if (!addOldAddress.IsSuccess) Assert.Fail(addOldAddress.Error ?? "Address change failed");

            _ = DbContext.Customers.Add(customer.EnsureValue());
            _ = await DbContext.SaveChangesAsync();

            // erste Bestellung mit alter Adresse
            var order1Date = new DateTime(2017, 3, 31, 0, 0, 0, DateTimeKind.Utc);
            Address deliveryAddr1 = Address.Create("Alte Strasse", "10", "8000", "Zürich", "CH").Value!;

            Result<Order> order1 = Order.Create(2041, "ORD-2017-001", customer.EnsureValue().Id, deliveryAddr1);
            if (!order1.IsSuccess) Assert.Fail(order1.Error ?? "Order creation failed");

            typeof(Order).GetProperty("OrderDate")!.SetValue(order1.EnsureValue(), order1Date);

            _ = DbContext.Orders.Add(order1.EnsureValue());
            _ = await DbContext.SaveChangesAsync();

            // Adressänderung - WICHTIG: Delay für temporale Tabelle
            await Task.Delay(100);

            Result changeAddress = customer.EnsureValue().ChangeAddress(
                DateOnly.FromDateTime(new DateTime(2017, 4, 2)),
                "Neue Strasse",
                "42",
                "8001",
                "Zürich",
                "CH");

            if (!changeAddress.IsSuccess) Assert.Fail(changeAddress.Error ?? "Address change failed");

            _ = await DbContext.SaveChangesAsync();

            // zweite Bestellung mit neuer Adresse
            await Task.Delay(100);

            var order2Date = new DateTime(2017, 4, 30, 0, 0, 0, DateTimeKind.Utc);
            Address deliveryAddr2 = Address.Create("Neue Strasse", "42", "8001", "Zürich", "CH").Value!;

            Result<Order> order2 = Order.Create(2369, "ORD-2017-002", customer.EnsureValue().Id, deliveryAddr2);
            if (!order2.IsSuccess) Assert.Fail(order2.Error ?? "Order creation failed");

            typeof(Order).GetProperty("OrderDate")!.SetValue(order2.EnsureValue(), order2Date);

            _ = DbContext.Orders.Add(order2.EnsureValue());
            _ = await DbContext.SaveChangesAsync();

            // Act
            var queryDate = new DateTime(2017, 5, 1, 0, 0, 0, DateTimeKind.Utc);
            IReadOnlyList<InvoiceDto> result = await _sut.GetOrdersWithHistoricalAddressAsync(
                fromDate: null,
                toDate: queryDate,
                customerNumber: null);

            // Assert
            Assert.AreEqual(2, result.Count, "Expected 2 invoices");

            // Erste Bestellung sollte alte Adresse haben
            InvoiceDto firstOrder = result.First(x => x.Rechnungsnummer == "ORD-2017-001");
            Assert.AreEqual("C-01234", firstOrder.Kundennummer, "Expected customer number C-01234");
            Assert.AreEqual("Müller Hans", firstOrder.Name, "Expected customer name 'Müller Hans'");
            Assert.AreEqual("Alte Strasse 10", firstOrder.Strasse, "Expected old street address");
            Assert.AreEqual("8000", firstOrder.PLZ, "Expected postal code 8000");
            Assert.AreEqual("Zürich", firstOrder.Ort, "Expected city Zürich");
            Assert.AreEqual("Schweiz", firstOrder.Land, "Expected country Schweiz");
            Assert.AreEqual(order1Date, firstOrder.Rechnungsdatum, "Expected order1Date");

            // Zweite Bestellung sollte neue Adresse haben
            InvoiceDto secondOrder = result.First(x => x.Rechnungsnummer == "ORD-2017-002");
            Assert.AreEqual("C-01234", secondOrder.Kundennummer, "Expected customer number C-01234");
            Assert.AreEqual("Neue Strasse 42", secondOrder.Strasse, "Expected new street address");
            Assert.AreEqual("8001", secondOrder.PLZ, "Expected postal code 8001");
            Assert.AreEqual(order2Date, secondOrder.Rechnungsdatum, "Expected order2Date");
        }

        [TestMethod]
        public async Task GetOrdersWithHistoricalAddress_WithCustomerFilter_ReturnsOnlyMatchingOrders()
        {
            // Arrange
            Result<Customer> customer1 = Customer.Create(100, "C-00100", "A", "Firma", "a@test.ch", null, "hash1");
            if (!customer1.IsSuccess) Assert.Fail(customer1.Error);

            Result addr1 = customer1.EnsureValue().ChangeAddress(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                "Str",
                "1",
                "1000",
                "CityA",
                "CH");
            if (!addr1.IsSuccess) Assert.Fail(addr1.Error);

            Result<Customer> customer2 = Customer.Create(200, "C-00200", "B", "Firma", "b@test.ch", null, "hash2");
            if (!customer2.IsSuccess) Assert.Fail(customer2.Error);

            Result addr2 = customer2.EnsureValue().ChangeAddress(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                "Str",
                "2",
                "2000",
                "CityB",
                "CH");
            if (!addr2.IsSuccess) Assert.Fail(addr2.Error);

            DbContext.Customers.AddRange(customer1.EnsureValue(), customer2.EnsureValue());
            _ = await DbContext.SaveChangesAsync();

            Address addr = Address.Create("Test", "1", "1000", "City", "CH").Value!;
            Result<Order> order1 = Order.Create(3001, "ORD-2024-001", customer1.EnsureValue().Id, addr);
            Result<Order> order2 = Order.Create(3002, "ORD-2024-002", customer2.EnsureValue().Id, addr);

            if (!order1.IsSuccess) Assert.Fail(order1.Error);
            if (!order2.IsSuccess) Assert.Fail(order2.Error);

            _ = DbContext.Orders.Add(order1.EnsureValue());
            _ = await DbContext.SaveChangesAsync();
            DbContext.Entry(order1.EnsureValue()).State = EntityState.Detached;

            _ = DbContext.Orders.Add(order2.EnsureValue());
            _ = await DbContext.SaveChangesAsync();
            DbContext.Entry(order2.EnsureValue()).State = EntityState.Detached;

            // Act
            IReadOnlyList<InvoiceDto> result = await _sut.GetOrdersWithHistoricalAddressAsync(customerNumber: "C-00100");

            // Assert
            Assert.AreEqual(1, result.Count, "Expected exactly 1 invoice for customer C-00100");
            Assert.AreEqual("C-00100", result[0].Kundennummer, "Expected customer number C-00100");
        }

        [TestMethod]
        public async Task GetOrdersWithHistoricalAddress_WithDateFilter_ReturnsOnlyOrdersInRange()
        {
            // Arrange
            Result<Customer> customer = Customer.Create(300, "C-00300", "Test", "Firma", "test@test.ch", null, "hash");
            if (!customer.IsSuccess) Assert.Fail(customer.Error);

            Result addr = customer.Value!.ChangeAddress(
                DateOnly.FromDateTime(new DateTime(2017, 1, 1)),
                "Test Str",
                "1",
                "3000",
                "TestCity",
                "CH");
            if (!addr.IsSuccess) Assert.Fail(addr.Error);

            _ = DbContext.Customers.Add(customer.Value);
            _ = await DbContext.SaveChangesAsync();

            Address deliveryAddr = Address.Create("Test", "1", "3000", "City", "CH").Value!;

            Result<Order> order1 = Order.Create(4010, "ORD-2017-010", customer.Value.Id, deliveryAddr);
            Result<Order> order2 = Order.Create(4020, "ORD-2017-020", customer.Value.Id, deliveryAddr);
            Result<Order> order3 = Order.Create(4030, "ORD-2017-030", customer.Value.Id, deliveryAddr);

            if (!order1.IsSuccess) Assert.Fail(order1.Error);
            if (!order2.IsSuccess) Assert.Fail(order2.Error);
            if (!order3.IsSuccess) Assert.Fail(order3.Error);

            typeof(Order).GetProperty("OrderDate")!.SetValue(order1.Value, new DateTime(2017, 1, 15, 0, 0, 0, DateTimeKind.Utc));
            typeof(Order).GetProperty("OrderDate")!.SetValue(order2.Value, new DateTime(2017, 6, 15, 0, 0, 0, DateTimeKind.Utc));
            typeof(Order).GetProperty("OrderDate")!.SetValue(order3.Value, new DateTime(2017, 12, 15, 0, 0, 0, DateTimeKind.Utc));

            _ = DbContext.Orders.Add(order1.Value!);
            _ = await DbContext.SaveChangesAsync();
            DbContext.Entry(order1.Value!).State = EntityState.Detached;

            _ = DbContext.Orders.Add(order2.Value!);
            _ = await DbContext.SaveChangesAsync();
            DbContext.Entry(order2.Value!).State = EntityState.Detached;

            _ = DbContext.Orders.Add(order3.Value!);
            _ = await DbContext.SaveChangesAsync();
            DbContext.Entry(order3.Value!).State = EntityState.Detached;

            // Act
            var fromDate = new DateTime(2017, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var toDate = new DateTime(2017, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            IReadOnlyList<InvoiceDto> result = await _sut.GetOrdersWithHistoricalAddressAsync(fromDate, toDate);

            // Assert
            Assert.AreEqual(1, result.Count, "Expected exactly 1 order in date range");
            Assert.AreEqual("ORD-2017-020", result[0].Rechnungsnummer, "Expected order number ORD-2017-020");
        }

        [TestMethod]
        public async Task GetOrdersWithHistoricalAddress_NoOrders_ReturnsEmptyList()
        {
            // Act
            IReadOnlyList<InvoiceDto> result = await _sut.GetOrdersWithHistoricalAddressAsync();

            // Assert
            Assert.AreEqual(0, result.Count, "Expected empty result list");
            Assert.IsFalse(result.Any(), "Expected no invoices");
        }
    }
}
