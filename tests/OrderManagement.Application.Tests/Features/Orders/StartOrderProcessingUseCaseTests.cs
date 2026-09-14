using OrderManagement.Application.Features.Orders.StartOrderProcessing;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class StartOrderProcessingUseCaseTests
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

        [TestMethod]
        public async Task ExecuteAsync_WithOpenOrder_ShouldTransitionToInProgressAndCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new StartOrderProcessingUseCase(orderCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Order order = orderCommandRepository.Seed(CreateOrder());

            Result result = await useCase.ExecuteAsync(new StartOrderProcessingCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(OrderStatus.InProgress, order.Status);
            Assert.AreEqual(1, orderCommandRepository.Updated.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_UsesDomainErrorFromInvalidTransition()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new StartOrderProcessingUseCase(orderCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Order order = CreateOrder();
            order.Cancel(FixedNow.UtcDateTime).EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new StartOrderProcessingCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
            Assert.AreEqual(0, orderCommandRepository.Updated.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCommitFailure_ShouldReturnFailure()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork { FailureMessage = "Concurrency conflict." };
            var useCase = new StartOrderProcessingUseCase(orderCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Order order = orderCommandRepository.Seed(CreateOrder());

            Result result = await useCase.ExecuteAsync(new StartOrderProcessingCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Concurrency conflict.", result.Error);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new StartOrderProcessingUseCase(orderCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Result result = await useCase.ExecuteAsync(new StartOrderProcessingCommand(999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
