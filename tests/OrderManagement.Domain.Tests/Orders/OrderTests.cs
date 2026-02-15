using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Domain.Tests.Orders
{
    [TestClass]
    public class OrderTests
    {
        private Address _testAddress = default!;
        private CustomerId _testCustomerId;

        [TestInitialize]
        public void Setup()
        {
            _testAddress = Address.Create("Teststrasse", "1", "8000", "Zürich", "CH").EnsureValue();
            _testCustomerId = new CustomerId(1);
        }

        [TestMethod]
        public void CreateShouldRaiseOrderCreatedEvent()
        {
            // Act
            Result<Order> result = Order.Create(
                id: 100,
                orderNumber: "ORD-2025-001",
                customerId: _testCustomerId,
                deliveryAddress: _testAddress);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Order order = result.Value!;
            Assert.AreEqual("ORD-2025-001", order.OrderNumber.Value);
            Assert.AreEqual(1, order.DomainEvents.Count); // Prüft das OrderCreated Event
        }

        [TestMethod]
        public void AddLineShouldIncreaseTotalAndAddLine()
        {
            // Arrange
            Order order = Order.Create(1, "ORD-2025-001", _testCustomerId, _testAddress).Value!;
            Money price = Money.From(50.50m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                articleId: 500,
                articleName: "Test Artikel",
                unitPrice: price,
                quantity: 2);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, order.Lines.Count);
            Assert.AreEqual(101.00m, order.Total.Amount);
            Assert.AreEqual("CHF", order.Total.Currency);
        }

        [TestMethod]
        public void AddLineWithMultiplePositionsShouldSumUpTotal()
        {
            // Arrange
            Order order = Order.Create(1, "ORD-2025-001", _testCustomerId, _testAddress).Value!;
            Money price1 = Money.From(10.00m, "CHF").EnsureValue();
            Money price2 = Money.From(25.50m, "CHF").EnsureValue();

            // Act
            _ = order.AddLine(1, "Artikel 1", price1, 1);
            _ = order.AddLine(2, "Artikel 2", price2, 2);

            // Assert
            Assert.AreEqual(61.00m, order.Total.Amount);
            Assert.AreEqual(2, order.Lines.Count);
        }

        [TestMethod]
        public void AddLineShouldFailWhenQuantityIsZeroOrNegative()
        {
            // Arrange
            Order order = Order.Create(1, "ORD-2025-001", _testCustomerId, _testAddress).Value!;
            Money price = Money.From(10.00m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(1, "Artikel", price, 0);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, order.Lines.Count);
        }

        [TestMethod]
        public void CreateShouldFailWhenOrderNumberIsEmpty()
        {
            // Act
            Result<Order> result = Order.Create(1, "", _testCustomerId, _testAddress);

            // Assert
            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void AddLineShouldFailWhenCurrenciesDoNotMatch()
        {
            // Arrange
            Order order = Order.Create(1, "ORD-2025-001", _testCustomerId, _testAddress).Value!;
            _ = order.AddLine(1, "Artikel CHF", Money.From(10, "CHF").EnsureValue(), 1);

            // Act
            Result result = order.AddLine(2, "Artikel EUR", Money.From(10, "EUR").EnsureValue(), 1);

            // Assert
            Assert.IsFalse(result.IsSuccess, "Unterschiedliche Währungen sollten nicht erlaubt sein.");
        }

        [TestMethod]
        public void AddressCreateShouldFailWhenStreetIsEmpty()
        {
            // Act
            Result<Address> result = Address.Create("", "10", "8000", "Zürich", "CH");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Street is required.", result.Error);
        }

        [TestMethod]
        public void AddressesWithSameValuesShouldBeEqual()
        {
            // Arrange
            Address addr1 = Address.Create("Bahnhofstr", "1", "8001", "ZH", "CH").Value!;
            Address addr2 = Address.Create("Bahnhofstr", "1", "8001", "ZH", "CH").Value!;

            // Assert
            Assert.AreEqual(addr1, addr2);
        }
    }
}
