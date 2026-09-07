using OrderManagement.Application.Features.Orders.CompleteOrder;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class CompleteOrderUseCaseTests
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);

        private static Order CreateOrder(string orderNumber = "ORD-2026-001")
            => Order.Create(
                    orderNumber,
                    new CustomerId(1),
                    new DateOnly(2026, 9, 1),
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                    AddressSource.Automatic,
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                    AddressSource.Automatic)
                .EnsureValue();

        private static Order OrderInProgressWithLine()
        {
            Order order = CreateOrder();
            _ = order.AddLine(new Domain.Catalog.ValueObjects.ArticleId(1), "Widget", Money.From(10m, "CHF").EnsureValue(), 1);
            order.MarkInventoryApplied().EnsureSuccess();
            order.StartProcessing(FixedNow.UtcDateTime).EnsureSuccess();
            return order;
        }

        [TestMethod]
        public async Task ExecuteAsync_WithInProgressOrder_ShouldTransitionToCompletedAndCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CompleteOrderUseCase(orderCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Order order = orderCommandRepository.Seed(OrderInProgressWithLine());

            Result result = await useCase.ExecuteAsync(new CompleteOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(OrderStatus.Completed, order.Status);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithStillOpenOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CompleteOrderUseCase(orderCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Order order = orderCommandRepository.Seed(CreateOrder());

            Result result = await useCase.ExecuteAsync(new CompleteOrderCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCommitFailure_ShouldReturnFailure()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork { FailureMessage = "Concurrency conflict." };
            var useCase = new CompleteOrderUseCase(orderCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Order order = orderCommandRepository.Seed(OrderInProgressWithLine());

            Result result = await useCase.ExecuteAsync(new CompleteOrderCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Concurrency conflict.", result.Error);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CompleteOrderUseCase(orderCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Result result = await useCase.ExecuteAsync(new CompleteOrderCommand(999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
