using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.AddOrderLine;
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
    public sealed class AddOrderLineUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingOrderAndArticle_ShouldAddLineAndRecalculateTotal()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new AddOrderLineUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = orderCommandRepository.Seed(ValidOrder());
            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            Result result = await useCase.ExecuteAsync(new AddOrderLineCommand(order.Id.Value, article.Id.Value, 4));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, order.Lines.Count);
            Assert.AreEqual(40m, order.Total.Amount);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithValidLine_ShouldReduceArticleStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new AddOrderLineUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = orderCommandRepository.Seed(ValidOrder());
            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            Result result = await useCase.ExecuteAsync(new AddOrderLineCommand(order.Id.Value, article.Id.Value, 4));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(6, article.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithQuantityExceedingStock_ShouldFailAndNotAddLine()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new AddOrderLineUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = orderCommandRepository.Seed(ValidOrder());
            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 2).EnsureValue());

            Result result = await useCase.ExecuteAsync(new AddOrderLineCommand(order.Id.Value, article.Id.Value, 5));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "stock");
            Assert.AreEqual(2, article.Stock);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new AddOrderLineUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new AddOrderLineCommand(999, 1, 1));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownArticle_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new AddOrderLineUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = orderCommandRepository.Seed(ValidOrder());

            Result result = await useCase.ExecuteAsync(new AddOrderLineCommand(order.Id.Value, 999, 1));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, order.Lines.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithZeroQuantity_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new AddOrderLineUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = orderCommandRepository.Seed(ValidOrder());
            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1)).EnsureValue());

            Result result = await useCase.ExecuteAsync(new AddOrderLineCommand(order.Id.Value, article.Id.Value, 0));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDeactivatedArticle_ShouldFailAndNotAddLine()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new AddOrderLineUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Order order = orderCommandRepository.Seed(ValidOrder());
            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());
            article.Deactivate().EnsureSuccess();

            Result result = await useCase.ExecuteAsync(new AddOrderLineCommand(order.Id.Value, article.Id.Value, 1));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, order.Lines.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        private static Order ValidOrder() => Order.Create(
                "ORD-2026-001",
                new CustomerId(1),
                new DateOnly(2026, 9, 1),
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic)
            .EnsureValue();
    }
}
