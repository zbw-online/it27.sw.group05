using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.Events;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Tests.Domain.Orders
{
    [TestClass]
    public sealed class OrderTests
    {
        [TestMethod]
        public void Create_WithValidData_CreatesTransientOrder()
        {
            // Arrange
            Address billingAddress = ValidAddress();
            Address deliveryAddress = ValidAddress();
            CustomerId customerId = new(42);
            DateOnly deliveryDate = new(2026, 9, 6);

            DateTime before = DateTime.UtcNow;

            // Act
            Result<Order> result = Order.Create(
                "ORD-2026-001",
                customerId,
                deliveryDate,
                billingAddress,
                AddressSource.Automatic,
                deliveryAddress,
                AddressSource.Automatic);

            DateTime after = DateTime.UtcNow;

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);

            Order order = result.EnsureValue();

            Assert.AreEqual(OrderId.Empty, order.Id);
            Assert.IsFalse(order.Id.IsAssigned);

            Assert.AreEqual("ORD-2026-001", order.OrderNumber.Value);
            Assert.AreEqual(customerId, order.CustomerId);
            Assert.AreEqual(deliveryDate, order.DeliveryDate);
            Assert.AreEqual(billingAddress, order.BillingAddress);
            Assert.AreEqual(AddressSource.Automatic, order.BillingAddressSource);
            Assert.AreEqual(deliveryAddress, order.DeliveryAddress);
            Assert.AreEqual(AddressSource.Automatic, order.DeliveryAddressSource);
            Assert.IsNull(order.CustomerReference);

            Assert.AreEqual(0, order.Lines.Count);
            Assert.AreEqual(0m, order.Total.Amount);
            Assert.AreEqual("CHF", order.Total.Currency);

            Assert.IsTrue(order.OrderDate >= before);
            Assert.IsTrue(order.OrderDate <= after);

            Assert.IsFalse(order.IsInventoryApplied);
        }

        [TestMethod]
        public void MarkInventoryApplied_WhenNotYetApplied_ShouldSucceed()
        {
            Order order = ValidOrder();

            Result result = order.MarkInventoryApplied();

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(order.IsInventoryApplied);
        }

        [TestMethod]
        public void MarkInventoryApplied_WhenAlreadyApplied_ShouldFail()
        {
            Order order = ValidOrder();
            order.MarkInventoryApplied().EnsureSuccess();

            Result result = order.MarkInventoryApplied();

            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(order.IsInventoryApplied);
        }

        [TestMethod]
        public void Create_WithManualOverridesAndDifferentAddresses_StoresBothIndependently()
        {
            // Arrange
            Result<Address> billingResult = Address.Create("Rechnungsweg", "1", "8000", "Zurich", "CH");
            Result<Address> deliveryResult = Address.Create("Lieferweg", "2", "9000", "St. Gallen", "CH");

            // Act
            Result<Order> result = Order.Create(
                "ORD-2026-010",
                new CustomerId(1),
                new DateOnly(2026, 9, 1),
                billingResult.EnsureValue(),
                AddressSource.Manual,
                deliveryResult.EnsureValue(),
                AddressSource.Manual,
                "  Projekt XY  ");

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);

            Order order = result.EnsureValue();

            Assert.AreEqual(billingResult.EnsureValue(), order.BillingAddress);
            Assert.AreEqual(deliveryResult.EnsureValue(), order.DeliveryAddress);
            Assert.AreNotEqual(order.BillingAddress, order.DeliveryAddress);
            Assert.AreEqual(AddressSource.Manual, order.BillingAddressSource);
            Assert.AreEqual(AddressSource.Manual, order.DeliveryAddressSource);
            Assert.AreEqual("Projekt XY", order.CustomerReference);
        }

        [TestMethod]
        public void Create_WithBlankCustomerReference_StoresNull()
        {
            // Act
            Result<Order> result = Order.Create(
                "ORD-2026-011",
                new CustomerId(1),
                new DateOnly(2026, 9, 1),
                ValidAddress(),
                AddressSource.Automatic,
                ValidAddress(),
                AddressSource.Automatic,
                "   ");

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsNull(result.EnsureValue().CustomerReference);
        }

        [TestMethod]
        public void Create_WithTooLongCustomerReference_ReturnsFailure()
        {
            // Act
            Result<Order> result = Order.Create(
                "ORD-2026-012",
                new CustomerId(1),
                new DateOnly(2026, 9, 1),
                ValidAddress(),
                AddressSource.Automatic,
                ValidAddress(),
                AddressSource.Automatic,
                new string('X', 101));

            // Assert
            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "CustomerReference");
        }

        [TestMethod]
        public void Create_WithValidData_AddsOrderCreatedDomainEvent()
        {
            // Arrange
            Address deliveryAddress = ValidAddress();

            // Act
            Result<Order> result = Order.Create(
                "ORD-2026-002",
                new CustomerId(1),
                new DateOnly(2026, 9, 1),
                deliveryAddress,
                AddressSource.Automatic,
                deliveryAddress,
                AddressSource.Automatic);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);

            Order order = result.EnsureValue();

            Assert.AreEqual(1, order.DomainEvents.Count);

            OrderCreated? createdEvent = order.DomainEvents
                .OfType<OrderCreated>()
                .SingleOrDefault();

            Assert.IsNotNull(createdEvent);
            Assert.AreEqual("ORD-2026-002", createdEvent.OrderNumber.Value);
        }

        [TestMethod]
        public void Create_WithInvalidOrderNumber_ReturnsFailure()
        {
            // Arrange
            Address deliveryAddress = ValidAddress();

            // Act
            Result<Order> result = Order.Create(
                "INVALID",
                new CustomerId(1),
                new DateOnly(2026, 9, 1),
                deliveryAddress,
                AddressSource.Automatic,
                deliveryAddress,
                AddressSource.Automatic);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(result.Value);
            StringAssert.Contains(result.Error!, "Order number");
        }

        [TestMethod]
        public void Create_WithUnassignedCustomerId_ReturnsFailure()
        {
            // Arrange
            Address deliveryAddress = ValidAddress();

            // Act
            Result<Order> result = Order.Create(
                "ORD-2026-003",
                CustomerId.Empty,
                new DateOnly(2026, 9, 1),
                deliveryAddress,
                AddressSource.Automatic,
                deliveryAddress,
                AddressSource.Automatic);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(result.Value);
            StringAssert.Contains(result.Error!, "CustomerId must be assigned");
        }

        [TestMethod]
        public void AddLine_WithValidArticle_AddsLine()
        {
            // Arrange
            Order order = ValidOrder();
            ArticleId articleId = new(10);
            Money unitPrice = Money.From(25.50m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                articleId,
                "Oak Tabletop",
                unitPrice,
                2);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);

            Assert.AreEqual(1, order.Lines.Count);

            OrderLine line = order.Lines.Single();

            Assert.AreEqual(1, line.LineNumber);
            Assert.AreEqual(articleId, line.ArticleId);
            Assert.AreEqual("Oak Tabletop", line.ArticleName);
            Assert.AreEqual(25.50m, line.UnitPrice.Amount);
            Assert.AreEqual("CHF", line.UnitPrice.Currency);
            Assert.AreEqual(2, line.Quantity);
            Assert.AreEqual(51.00m, line.LineTotal.Amount);
            Assert.AreEqual("CHF", line.LineTotal.Currency);
        }

        [TestMethod]
        public void AddLine_WithValidArticle_RecalculatesTotal()
        {
            // Arrange
            Order order = ValidOrder();

            Money firstPrice = Money.From(10m, "CHF").EnsureValue();
            Money secondPrice = Money.From(15.50m, "CHF").EnsureValue();

            // Act
            Result firstResult = order.AddLine(
                new ArticleId(1),
                "Article One",
                firstPrice,
                3);

            Result secondResult = order.AddLine(
                new ArticleId(2),
                "Article Two",
                secondPrice,
                2);

            // Assert
            Assert.IsTrue(firstResult.IsSuccess, firstResult.Error);
            Assert.IsTrue(secondResult.IsSuccess, secondResult.Error);

            Assert.AreEqual(2, order.Lines.Count);

            Assert.AreEqual(61.00m, order.Total.Amount);
            Assert.AreEqual("CHF", order.Total.Currency);
        }

        [TestMethod]
        public void AddLine_WithMultipleLines_AssignsSequentialLineNumbers()
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
        public void AddLine_WithUnassignedArticleId_ReturnsFailure()
        {
            // Arrange
            Order order = ValidOrder();
            Money unitPrice = Money.From(10m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                ArticleId.Empty,
                "Invalid Article",
                unitPrice,
                1);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "ArticleId must be assigned");

            Assert.AreEqual(0, order.Lines.Count);
            Assert.AreEqual(0m, order.Total.Amount);
        }

        [TestMethod]
        public void AddLine_WithEmptyArticleName_ReturnsFailure()
        {
            // Arrange
            Order order = ValidOrder();
            Money unitPrice = Money.From(10m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                new ArticleId(1),
                "   ",
                unitPrice,
                1);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "ArticleName is required");

            Assert.AreEqual(0, order.Lines.Count);
            Assert.AreEqual(0m, order.Total.Amount);
        }

        [TestMethod]
        public void AddLine_WithZeroQuantity_ReturnsFailure()
        {
            // Arrange
            Order order = ValidOrder();
            Money unitPrice = Money.From(10m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                new ArticleId(1),
                "Article",
                unitPrice,
                0);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "Quantity must be positive");

            Assert.AreEqual(0, order.Lines.Count);
            Assert.AreEqual(0m, order.Total.Amount);
        }

        [TestMethod]
        public void AddLine_WithNegativeQuantity_ReturnsFailure()
        {
            // Arrange
            Order order = ValidOrder();
            Money unitPrice = Money.From(10m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                new ArticleId(1),
                "Article",
                unitPrice,
                -1);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "Quantity must be positive");

            Assert.AreEqual(0, order.Lines.Count);
            Assert.AreEqual(0m, order.Total.Amount);
        }

        [TestMethod]
        public void AddLine_WithDifferentCurrencyThanExistingLines_ReturnsFailure()
        {
            // Arrange
            Order order = ValidOrder();

            Money chfPrice = Money.From(10m, "CHF").EnsureValue();
            Money eurPrice = Money.From(10m, "EUR").EnsureValue();

            Result firstResult = order.AddLine(
                new ArticleId(1),
                "CHF Article",
                chfPrice,
                2);

            Assert.IsTrue(firstResult.IsSuccess, firstResult.Error);

            // Act
            Result secondResult = order.AddLine(
                new ArticleId(2),
                "EUR Article",
                eurPrice,
                1);

            // Assert
            Assert.IsFalse(secondResult.IsSuccess);
            StringAssert.Contains(secondResult.Error!, "Invalid currency");

            Assert.AreEqual(1, order.Lines.Count);
            Assert.AreEqual(20m, order.Total.Amount);
            Assert.AreEqual("CHF", order.Total.Currency);
        }

        [TestMethod]
        public void AddLine_TrimsArticleName()
        {
            // Arrange
            Order order = ValidOrder();
            Money unitPrice = Money.From(10m, "CHF").EnsureValue();

            // Act
            Result result = order.AddLine(
                new ArticleId(1),
                "   Oak Panel   ",
                unitPrice,
                1);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);

            OrderLine line = order.Lines.Single();

            Assert.AreEqual("Oak Panel", line.ArticleName);
        }

        [TestMethod]
        public void UpdateLineQuantity_WithExistingLineAndValidQuantity_UpdatesQuantityAndRecalculatesTotal()
        {
            // Arrange
            Order order = ValidOrder();
            Money unitPrice = Money.From(10m, "CHF").EnsureValue();
            _ = order.AddLine(new ArticleId(1), "Article", unitPrice, 2);

            OrderLine line = order.Lines.Single();
            AssignLineId(line, 100);

            // Act
            Result result = order.UpdateLineQuantity(line.Id, 5);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, line.Quantity);
            Assert.AreEqual(50m, line.LineTotal.Amount);
            Assert.AreEqual(50m, order.Total.Amount);
        }

        [TestMethod]
        public void UpdateLineQuantity_WithUnknownLineId_ReturnsFailure()
        {
            // Arrange
            Order order = ValidOrder();
            Money unitPrice = Money.From(10m, "CHF").EnsureValue();
            _ = order.AddLine(new ArticleId(1), "Article", unitPrice, 2);

            // Act
            Result result = order.UpdateLineQuantity(new OrderLineId(999), 5);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "not found");
        }

        [TestMethod]
        public void UpdateLineQuantity_WithZeroQuantity_ReturnsFailureAndLeavesLineUnchanged()
        {
            // Arrange
            Order order = ValidOrder();
            Money unitPrice = Money.From(10m, "CHF").EnsureValue();
            _ = order.AddLine(new ArticleId(1), "Article", unitPrice, 2);

            OrderLine line = order.Lines.Single();
            AssignLineId(line, 100);

            // Act
            Result result = order.UpdateLineQuantity(line.Id, 0);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(2, line.Quantity);
            Assert.AreEqual(20m, order.Total.Amount);
        }

        [TestMethod]
        public void RemoveLine_WithExistingLine_RemovesLineAndRecalculatesTotal()
        {
            // Arrange
            Order order = ValidOrder();
            Money price = Money.From(10m, "CHF").EnsureValue();
            _ = order.AddLine(new ArticleId(1), "First", price, 1);
            _ = order.AddLine(new ArticleId(2), "Second", price, 2);

            OrderLine[] lines = [.. order.Lines.OrderBy(l => l.LineNumber)];
            AssignLineId(lines[0], 100);
            AssignLineId(lines[1], 101);

            // Act
            Result result = order.RemoveLine(lines[0].Id);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, order.Lines.Count);
            Assert.AreEqual("Second", order.Lines.Single().ArticleName);
            Assert.AreEqual(20m, order.Total.Amount);
        }

        [TestMethod]
        public void RemoveLine_WithUnknownLineId_ReturnsFailure()
        {
            // Arrange
            Order order = ValidOrder();
            Money price = Money.From(10m, "CHF").EnsureValue();
            _ = order.AddLine(new ArticleId(1), "Article", price, 1);

            // Act
            Result result = order.RemoveLine(new OrderLineId(999));

            // Assert
            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "not found");
            Assert.AreEqual(1, order.Lines.Count);
        }

        [TestMethod]
        public void AddLine_AfterRemovingAnEarlierLine_DoesNotReuseLineNumber()
        {
            // Arrange
            Order order = ValidOrder();
            Money price = Money.From(10m, "CHF").EnsureValue();
            _ = order.AddLine(new ArticleId(1), "First", price, 1);
            _ = order.AddLine(new ArticleId(2), "Second", price, 1);

            OrderLine firstLine = order.Lines.OrderBy(l => l.LineNumber).First();
            AssignLineId(firstLine, 100);
            _ = order.RemoveLine(firstLine.Id);

            // Act
            Result result = order.AddLine(new ArticleId(3), "Third", price, 1);

            // Assert
            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(3, order.Lines.Single(l => l.ArticleName == "Third").LineNumber);
        }

        private static void AssignLineId(OrderLine line, int id)
            => typeof(OrderLine).GetProperty(nameof(OrderLine.Id))!.SetValue(line, new OrderLineId(id));

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
