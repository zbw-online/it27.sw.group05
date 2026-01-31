using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using SharedKernel.ValueObjects;
using SharedKernel.Primitives;

namespace OrderManagement.Domain.Tests.Orders
{
    [TestClass]
    public class OrderTests
    {
        private Address _testAddress;
        private CustomerId _testCustomerId;

        [TestInitialize]
        public void Setup()
        {
            // Hilfsobjekte für die Tests vorbereiten
            _testAddress = Address.Create("Teststrasse", "1", "8000", "Zürich", "CH").Value!;
            _testCustomerId = new CustomerId(1);
        }

        [TestMethod]
        public void Create_ShouldRaiseOrderCreatedEvent()
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
            Assert.AreEqual("ORD-2025-001", order.OrderNumber);
            Assert.AreEqual(1, order.DomainEvents.Count); // Prüft das OrderCreated Event
        }

        [TestMethod]
        public void AddLine_ShouldIncreaseTotalAndAddLine()
        {
            // Arrange
            Order order = Order.Create(1, "ORD-001", _testCustomerId, _testAddress).Value!;
            Money price = Money.Create(50.50m, "CHF").Value!;

            // Act
            Result result = order.AddLine(
                articleId: 500,
                articleName: "Test Artikel",
                unitPrice: price,
                quantity: 2);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, order.Lines.Count);
            Assert.AreEqual(101.00m, order.Total.Amount); // 50.50 * 2
            Assert.AreEqual("CHF", order.Total.Currency);
        }

        [TestMethod]
        public void AddLine_WithMultiplePositions_ShouldSumUpTotal()
        {
            // Arrange
            Order order = Order.Create(1, "ORD-001", _testCustomerId, _testAddress).Value!;
            Money price1 = Money.Create(10.00m, "CHF").Value!;
            Money price2 = Money.Create(25.50m, "CHF").Value!;

            // Act
            order.AddLine(1, "Artikel 1", price1, 1);
            order.AddLine(2, "Artikel 2", price2, 2);

            // Assert
            // 10.00 + (2 * 25.50) = 61.00
            Assert.AreEqual(61.00m, order.Total.Amount);
            Assert.AreEqual(2, order.Lines.Count);
        }

        [TestMethod]
        public void AddLine_ShouldFail_WhenQuantityIsZeroOrNegative()
        {
            // Arrange
            Order order = Order.Create(1, "ORD-001", _testCustomerId, _testAddress).Value!;
            Money price = Money.Create(10.00m, "CHF").Value!;

            // Act
            Result result = order.AddLine(1, "Artikel", price, 0);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, order.Lines.Count);
        }

        [TestMethod]
        public void Create_ShouldFail_WhenOrderNumberIsEmpty()
        {
            // Act
            Result<Order> result = Order.Create(1, "", _testCustomerId, _testAddress);

            // Assert
            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void AddLine_ShouldFail_WhenCurrenciesDoNotMatch()
        {
            // Arrange
            Order order = Order.Create(1, "ORD-001", _testCustomerId, _testAddress).Value!;
            order.AddLine(1, "Artikel CHF", Money.Create(10, "CHF").Value!, 1);

            // Act
            Result result = order.AddLine(2, "Artikel EUR", Money.Create(10, "EUR").Value!, 1);

            // Assert
            Assert.IsFalse(result.IsSuccess, "Unterschiedliche Währungen sollten nicht erlaubt sein.");
        }

        [TestMethod]
        public void Money_Create_ShouldFail_OnInvalidCurrencyCode()
        {
            // Act
            var result = Money.Create(100, "INVALID");

            // Assert
            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Address_Create_ShouldFail_WhenStreetIsEmpty()
        {
            // Act
            var result = Address.Create("", "10", "8000", "Zürich", "CH");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Street is required.", result.Error);
        }

        [TestMethod]
        public void Addresses_WithSameValues_ShouldBeEqual()
        {
            // Arrange
            var addr1 = Address.Create("Bahnhofstr", "1", "8001", "ZH", "CH").Value!;
            var addr2 = Address.Create("Bahnhofstr", "1", "8001", "ZH", "CH").Value!;

            // Assert
            Assert.AreEqual(addr1, addr2); // Dank 'ValueObject' Basisklasse oder 'record'
        }
    }
}
