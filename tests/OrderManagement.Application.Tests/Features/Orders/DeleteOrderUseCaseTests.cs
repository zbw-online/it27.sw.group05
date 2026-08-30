using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.DeleteOrder;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog;
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
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Order order = Order.Create(
                    "ORD-2026-001",
                    new CustomerId(1),
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue())
                .EnsureValue();

            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 1);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, orderCommandRepository.Removed.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithExistingOrderContainingLines_ShouldRestoreArticleStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Order order = Order.Create(
                    "ORD-2026-001",
                    new CustomerId(1),
                    Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue())
                .EnsureValue();

            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = orderCommandRepository.Seed(order);
            _ = article.UpdateStock(-3);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, article.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
