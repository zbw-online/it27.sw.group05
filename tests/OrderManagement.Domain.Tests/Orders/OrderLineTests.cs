using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Tests.Domain.Orders
{
    [TestClass]
    public sealed class OrderLineTests
    {
        [TestMethod]
        public void Order_AddLine_CreatesOrderLineWithExpectedSnapshotData()
        {
            // Arrange
            Order order = ValidOrder();

            ArticleId articleId = new(15);
            Money unitPrice = Money.From(125.50m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                articleId,
                "Oak Tabletop",
                unitPrice,
                3);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);

            OrderLine line = order.Lines.Single();

            Assert.AreEqual(OrderLineId.Empty, line.Id);
            Assert.IsFalse(line.Id.IsAssigned);

            Assert.AreEqual(1, line.LineNumber);
            Assert.AreEqual(articleId, line.ArticleId);
            Assert.AreEqual("Oak Tabletop", line.ArticleName);
            Assert.AreEqual(125.50m, line.UnitPrice.Amount);
            Assert.AreEqual("CHF", line.UnitPrice.Currency);
            Assert.AreEqual(3, line.Quantity);
        }

        [TestMethod]
        public void Order_AddLine_CalculatesLineTotalFromUnitPriceAndQuantity()
        {
            // Arrange
            Order order = ValidOrder();

            Money unitPrice = Money.From(19.90m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                new ArticleId(20),
                "Shelf Board",
                unitPrice,
                5);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);

            OrderLine line = order.Lines.Single();

            Assert.AreEqual(99.50m, line.LineTotal.Amount);
            Assert.AreEqual("CHF", line.LineTotal.Currency);
        }

        [TestMethod]
        public void Order_AddLine_UsesSameCurrencyForUnitPriceAndLineTotal()
        {
            // Arrange
            Order order = ValidOrder();

            Money unitPrice = Money.From(100m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                new ArticleId(30),
                "Wardrobe Door",
                unitPrice,
                2);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);

            OrderLine line = order.Lines.Single();

            Assert.AreEqual("CHF", line.UnitPrice.Currency);
            Assert.AreEqual("CHF", line.LineTotal.Currency);
        }

        [TestMethod]
        public void Order_AddLine_WithMultipleLines_CreatesOrderLinesWithSequentialLineNumbers()
        {
            // Arrange
            Order order = ValidOrder();

            Money price = Money.From(10m, "CHF").EnsureValue();

            // Act
            _ = order.AddLine(new ArticleId(1), "First Article", price, 1);
            _ = order.AddLine(new ArticleId(2), "Second Article", price, 1);
            _ = order.AddLine(new ArticleId(3), "Third Article", price, 1);

            // Assert
            OrderLine[] lines = [.. order.Lines.OrderBy(x => x.LineNumber)];

            Assert.AreEqual(3, lines.Length);

            Assert.AreEqual(1, lines[0].LineNumber);
            Assert.AreEqual(2, lines[1].LineNumber);
            Assert.AreEqual(3, lines[2].LineNumber);
        }

        [TestMethod]
        public void Order_AddLine_TrimsArticleNameBeforeCreatingOrderLine()
        {
            // Arrange
            Order order = ValidOrder();

            Money unitPrice = Money.From(10m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                new ArticleId(40),
                "   Oak Panel   ",
                unitPrice,
                1);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);

            OrderLine line = order.Lines.Single();

            Assert.AreEqual("Oak Panel", line.ArticleName);
        }

        private static Order ValidOrder() => Order.Create(
                    "ORD-2026-999",
                    new CustomerId(1),
                    new DateOnly(2026, 9, 1),
                    ValidAddress(),
                    AddressSource.Automatic,
                    ValidAddress(),
                    AddressSource.Automatic)
                .EnsureValue();

        private static Address ValidAddress() => Address.Create(
                    "Musterstrasse",
                    "10",
                    "9000",
                    "St. Gallen",
                    "CH")
                .EnsureValue();
    }
}
