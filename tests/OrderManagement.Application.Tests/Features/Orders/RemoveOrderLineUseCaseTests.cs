using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.RemoveOrderLine;
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
    public sealed class RemoveOrderLineUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingLine_ShouldRemoveLineAndRecalculateTotal()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new RemoveOrderLineUseCase(orderCommandRepository, unitOfWork);

            Order order = Order.Create(
                    "ORD-2026-001",
                    new CustomerId(1),
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue())
                .EnsureValue();

            _ = order.AddLine(new ArticleId(1), "First", Money.From(10m, "CHF").EnsureValue(), 1);
            _ = order.AddLine(new ArticleId(2), "Second", Money.From(20m, "CHF").EnsureValue(), 1);

            OrderLine[] lines = [.. order.Lines.OrderBy(l => l.LineNumber)];
            typeof(OrderLine).GetProperty(nameof(OrderLine.Id))!.SetValue(lines[0], new OrderLineId(1));
            typeof(OrderLine).GetProperty(nameof(OrderLine.Id))!.SetValue(lines[1], new OrderLineId(2));

            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new RemoveOrderLineCommand(order.Id.Value, lines[0].Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, order.Lines.Count);
            Assert.AreEqual(20m, order.Total.Amount);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new RemoveOrderLineUseCase(orderCommandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new RemoveOrderLineCommand(999, 1));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownLine_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new RemoveOrderLineUseCase(orderCommandRepository, unitOfWork);

            Order order = Order.Create(
                    "ORD-2026-001",
                    new CustomerId(1),
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue())
                .EnsureValue();

            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new RemoveOrderLineCommand(order.Id.Value, 999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
