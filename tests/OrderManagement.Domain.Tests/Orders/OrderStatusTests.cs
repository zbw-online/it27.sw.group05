using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.Events;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Tests.Domain.Orders
{
    [TestClass]
    public sealed class OrderStatusTests
    {
        private static readonly DateTime NowUtc = new(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void Create_NewOrder_IsOpen()
        {
            Order order = ValidOrder();

            Assert.AreEqual(OrderStatus.Open, order.Status);
            Assert.IsTrue(order.IsActive);
            Assert.IsFalse(order.IsArchived);
        }

        [TestMethod]
        public void StartProcessing_WhenOpen_TransitionsToInProgress()
        {
            Order order = ValidOrder();

            Result result = order.StartProcessing(NowUtc);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(OrderStatus.InProgress, order.Status);
            Assert.AreEqual(NowUtc, order.StatusChangedAtUtc);
            Assert.IsTrue(order.IsActive);
        }

        [TestMethod]
        public void StartProcessing_WhenOpen_AddsOrderStartedProcessingDomainEvent()
        {
            Order order = ValidOrder();

            _ = order.StartProcessing(NowUtc);

            OrderStartedProcessing? domainEvent = order.DomainEvents.OfType<OrderStartedProcessing>().SingleOrDefault();
            Assert.IsNotNull(domainEvent);
            Assert.AreEqual(order.OrderNumber, domainEvent.OrderNumber);
        }

        [TestMethod]
        public void Cancel_WhenOpen_TransitionsToCancelled()
        {
            Order order = ValidOrder();

            Result result = order.Cancel(NowUtc);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(OrderStatus.Cancelled, order.Status);
            Assert.IsTrue(order.IsArchived);
        }

        [TestMethod]
        public void Complete_WhenInProgressWithLinesAndInventoryApplied_TransitionsToCompleted()
        {
            Order order = OrderReadyToComplete();

            Result result = order.Complete(NowUtc);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(OrderStatus.Completed, order.Status);
            Assert.AreEqual(NowUtc, order.StatusChangedAtUtc);
            Assert.IsTrue(order.IsArchived);
        }

        [TestMethod]
        public void Complete_WhenInProgress_AddsOrderCompletedDomainEvent()
        {
            Order order = OrderReadyToComplete();

            _ = order.Complete(NowUtc);

            OrderCompleted? domainEvent = order.DomainEvents.OfType<OrderCompleted>().SingleOrDefault();
            Assert.IsNotNull(domainEvent);
        }

        [TestMethod]
        public void Cancel_WhenInProgress_TransitionsToCancelled()
        {
            Order order = ValidOrder();
            _ = order.AddLine(new ArticleId(1), "Article", Money.From(10m, "CHF").EnsureValue(), 1);
            _ = order.MarkInventoryApplied();
            _ = order.StartProcessing(NowUtc);

            Result result = order.Cancel(NowUtc);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(OrderStatus.Cancelled, order.Status);
        }

        [TestMethod]
        public void Complete_WhenCompletedAlready_IsTerminalAndRejectsFurtherCompletion()
        {
            Order order = OrderReadyToComplete();
            _ = order.Complete(NowUtc);

            Result result = order.Complete(NowUtc);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(OrderStatus.Completed, order.Status);
        }

        [TestMethod]
        public void Cancel_WhenCancelledAlready_IsTerminalAndRejectsFurtherCancellation()
        {
            Order order = ValidOrder();
            _ = order.Cancel(NowUtc);

            Result result = order.Cancel(NowUtc);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(OrderStatus.Cancelled, order.Status);
        }

        [TestMethod]
        public void Cancel_WhenCompleted_IsRejected()
        {
            Order order = OrderReadyToComplete();
            _ = order.Complete(NowUtc);

            Result result = order.Cancel(NowUtc);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(OrderStatus.Completed, order.Status);
        }

        [TestMethod]
        public void StartProcessing_WhenAlreadyInProgress_IsRejected()
        {
            Order order = ValidOrder();
            _ = order.StartProcessing(NowUtc);

            Result result = order.StartProcessing(NowUtc);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void StartProcessing_WhenCompleted_IsRejected()
        {
            Order order = OrderReadyToComplete();
            _ = order.Complete(NowUtc);

            Result result = order.StartProcessing(NowUtc);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void StartProcessing_WhenCancelled_IsRejected()
        {
            Order order = ValidOrder();
            _ = order.Cancel(NowUtc);

            Result result = order.StartProcessing(NowUtc);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Complete_WhenStillOpen_IsRejected()
        {
            Order order = ValidOrder();
            _ = order.AddLine(new ArticleId(1), "Article", Money.From(10m, "CHF").EnsureValue(), 1);
            _ = order.MarkInventoryApplied();

            Result result = order.Complete(NowUtc);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(OrderStatus.Open, order.Status);
        }

        [TestMethod]
        public void Complete_WithoutAnyLines_IsRejected()
        {
            Order order = ValidOrder();
            _ = order.MarkInventoryApplied();
            _ = order.StartProcessing(NowUtc);

            Result result = order.Complete(NowUtc);

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "Positionen");
        }

        [TestMethod]
        public void Complete_WithoutInventoryApplied_IsRejected()
        {
            Order order = ValidOrder();
            _ = order.AddLine(new ArticleId(1), "Article", Money.From(10m, "CHF").EnsureValue(), 1);
            _ = order.StartProcessing(NowUtc);

            Result result = order.Complete(NowUtc);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void DeliveryDateInThePast_DoesNotAutomaticallyCompleteTheOrder()
        {
            Order order = ValidOrder(new DateOnly(2020, 1, 1));

            Assert.AreEqual(OrderStatus.Open, order.Status);
        }

        [TestMethod]
        public void IsOverdue_WhenActiveAndDeliveryDateInThePast_ReturnsTrue()
        {
            Order order = ValidOrder(new DateOnly(2026, 1, 1));

            Assert.IsTrue(order.IsOverdue(new DateOnly(2026, 9, 7)));
        }

        [TestMethod]
        public void IsOverdue_WhenActiveAndDeliveryDateInTheFuture_ReturnsFalse()
        {
            Order order = ValidOrder(new DateOnly(2027, 1, 1));

            Assert.IsFalse(order.IsOverdue(new DateOnly(2026, 9, 7)));
        }

        [TestMethod]
        public void IsOverdue_WhenArchivedWithPastDeliveryDate_ReturnsFalse()
        {
            Order order = OrderReadyToComplete(new DateOnly(2020, 1, 1));
            _ = order.Complete(NowUtc);

            Assert.IsFalse(order.IsOverdue(new DateOnly(2026, 9, 7)));
        }

        [TestMethod]
        public void Cancel_TwiceInARow_OnlyReportsInventoryReversalOnce()
        {
            Order order = ValidOrder();
            _ = order.AddLine(new ArticleId(1), "Article", Money.From(10m, "CHF").EnsureValue(), 1);
            _ = order.MarkInventoryApplied();

            Result first = order.Cancel(NowUtc);
            Result second = order.Cancel(NowUtc);

            Assert.IsTrue(first.IsSuccess, first.Error);
            Assert.IsFalse(second.IsSuccess);
            Assert.IsFalse(order.IsInventoryApplied);

            OrderCancelled cancelledEvent = order.DomainEvents.OfType<OrderCancelled>().Single();
            Assert.IsTrue(cancelledEvent.InventoryReversalRequired);
        }

        [TestMethod]
        public void Cancel_WhenInventoryWasNeverApplied_DoesNotRequestInventoryReversal()
        {
            Order order = ValidOrder();

            _ = order.Cancel(NowUtc);

            OrderCancelled cancelledEvent = order.DomainEvents.OfType<OrderCancelled>().Single();
            Assert.IsFalse(cancelledEvent.InventoryReversalRequired);
        }

        private static Order OrderReadyToComplete(DateOnly? deliveryDate = null)
        {
            Order order = ValidOrder(deliveryDate);
            _ = order.AddLine(new ArticleId(1), "Article", Money.From(10m, "CHF").EnsureValue(), 1);
            _ = order.MarkInventoryApplied();
            _ = order.StartProcessing(NowUtc);
            return order;
        }

        private static Order ValidOrder(DateOnly? deliveryDate = null) => Order.Create(
                    "ORD-2026-999",
                    new CustomerId(1),
                    deliveryDate ?? new DateOnly(2026, 9, 1),
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
