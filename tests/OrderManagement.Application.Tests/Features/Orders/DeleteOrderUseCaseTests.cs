using OrderManagement.Application.Features.Orders.DeleteOrder;
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
    public sealed class DeleteOrderUseCaseTests
    {
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
        public async Task ExecuteAsync_WithExistingOrderContainingLines_ShouldRemoveOrderAndCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Order order = CreateOrder();
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

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = article.UpdateStock(-3);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, article.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMultipleDifferentArticles_ShouldRestoreEachArticleStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article articleA = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());
            Article articleB = articleCommandRepository.Seed(
                Article.Create("ART-002", "Gadget", 20m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            Order order = CreateOrder();
            _ = order.AddLine(articleA.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 2);
            _ = order.AddLine(articleB.Id, "Gadget", Money.From(20m, "CHF").EnsureValue(), 6);
            _ = articleA.UpdateStock(-2);
            _ = articleB.UpdateStock(-6);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(10, articleA.Stock);
            Assert.AreEqual(10, articleB.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDuplicateArticleLines_ShouldGroupAndSumQuantities()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 2);
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = article.UpdateStock(-5);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(10, article.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithInactiveArticle_ShouldStillRestoreStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());
            _ = article.Deactivate();

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = article.UpdateStock(-3);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, article.Stock);
            Assert.AreEqual(ArticleStatus.Inactive, article.Status);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithInventoryNotApplied_ShouldNotIncreaseStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = orderCommandRepository.Seed(order);

            Assert.IsFalse(order.IsInventoryApplied, "A freshly created order must not start with inventory applied.");

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, article.Stock);
            Assert.AreEqual(1, orderCommandRepository.Removed.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMissingArticle_ShouldFailWithoutRemovingOrder()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue();
            TestIdAssigner.Assign(article, new ArticleId(42));

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, orderCommandRepository.Removed.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithStockUpdateFailure_ShouldPreventRemoval()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: int.MaxValue - 1).EnsureValue());

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 5);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(int.MaxValue - 1, article.Stock);
            Assert.AreEqual(0, orderCommandRepository.Removed.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCommitFailure_ShouldReturnFailure()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork { FailureMessage = "Concurrency conflict." };
            var useCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Concurrency conflict.", result.Error);
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
