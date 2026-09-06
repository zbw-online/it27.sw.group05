using OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog;
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
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateOrderLineQuantityUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = ValidOrderWithLine(articleCommandRepository, out OrderLine line, stock: 90);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(
                new UpdateOrderLineQuantityCommand(order.Id.Value, line.Id.Value, 7));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(7, line.Quantity);
            Assert.AreEqual(70m, order.Total.Amount);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_IncreasingQuantity_ShouldReduceArticleStockFurther()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateOrderLineQuantityUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = ValidOrderWithLine(articleCommandRepository, out OrderLine line, stock: 90, quantity: 10);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(
                new UpdateOrderLineQuantityCommand(order.Id.Value, line.Id.Value, 15));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Article article = (await articleCommandRepository.GetByIdAsync(line.ArticleId))!;
            Assert.AreEqual(85, article.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_DecreasingQuantity_ShouldRestoreArticleStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateOrderLineQuantityUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = ValidOrderWithLine(articleCommandRepository, out OrderLine line, stock: 90, quantity: 10);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(
                new UpdateOrderLineQuantityCommand(order.Id.Value, line.Id.Value, 4));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Article article = (await articleCommandRepository.GetByIdAsync(line.ArticleId))!;
            Assert.AreEqual(96, article.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_IncreasingQuantityBeyondStock_ShouldFailAndNotCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateOrderLineQuantityUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = ValidOrderWithLine(articleCommandRepository, out OrderLine line, stock: 2, quantity: 2);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(
                new UpdateOrderLineQuantityCommand(order.Id.Value, line.Id.Value, 20));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "stock");
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateOrderLineQuantityUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new UpdateOrderLineQuantityCommand(999, 1, 5));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithZeroQuantity_ShouldFailAndLeaveLineUnchanged()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateOrderLineQuantityUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = ValidOrderWithLine(articleCommandRepository, out OrderLine line, stock: 90);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(
                new UpdateOrderLineQuantityCommand(order.Id.Value, line.Id.Value, 0));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(10, line.Quantity);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        private static Order ValidOrderWithLine(
            FakeArticleCommandRepository articleCommandRepository, out OrderLine line, int stock, int quantity = 10)
        {
            Order order = Order.Create(
                    "ORD-2026-001",
                    new CustomerId(1),
                    new DateOnly(2026, 9, 1),
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                    AddressSource.Automatic,
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                    AddressSource.Automatic)
                .EnsureValue();

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: stock).EnsureValue());

            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), quantity);
            line = order.Lines.Single();
            typeof(OrderLine).GetProperty(nameof(OrderLine.Id))!.SetValue(line, new OrderLineId(1));
            return order;
        }
    }
}
