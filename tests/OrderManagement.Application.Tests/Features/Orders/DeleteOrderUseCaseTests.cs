using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.DeleteOrder;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class DeleteOrderUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingOrderContainingLines_ShouldRemoveOrderAndCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, unitOfWork);

            Order order = Order.Create(
                    "ORD-2026-001",
                    new CustomerId(1),
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue())
                .EnsureValue();

            _ = order.AddLine(new ArticleId(1), "Widget", Money.From(10m, "CHF").EnsureValue(), 1);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, orderCommandRepository.Removed.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
