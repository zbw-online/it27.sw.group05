using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class UpdateOrderLineQuantityUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingLine_ShouldUpdateQuantityAndRecalculateTotal()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateOrderLineQuantityUseCase(orderCommandRepository, unitOfWork);

            Order order = ValidOrderWithLine(out OrderLine line);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(
                new UpdateOrderLineQuantityCommand(order.Id.Value, line.Id.Value, 7));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(7, line.Quantity);
            Assert.AreEqual(70m, order.Total.Amount);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateOrderLineQuantityUseCase(orderCommandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new UpdateOrderLineQuantityCommand(999, 1, 5));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithZeroQuantity_ShouldFailAndLeaveLineUnchanged()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateOrderLineQuantityUseCase(orderCommandRepository, unitOfWork);

            Order order = ValidOrderWithLine(out OrderLine line);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(
                new UpdateOrderLineQuantityCommand(order.Id.Value, line.Id.Value, 0));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(10, line.Quantity);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        private static Order ValidOrderWithLine(out OrderLine line)
        {
            Order order = Order.Create(
                    "ORD-2026-001",
                    new CustomerId(1),
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue())
                .EnsureValue();

            _ = order.AddLine(new ArticleId(1), "Widget", Money.From(10m, "CHF").EnsureValue(), 10);
            line = order.Lines.Single();
            typeof(OrderLine).GetProperty(nameof(OrderLine.Id))!.SetValue(line, new OrderLineId(1));
            return order;
        }
    }
}
