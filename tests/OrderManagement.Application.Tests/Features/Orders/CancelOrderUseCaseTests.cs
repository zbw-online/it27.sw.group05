using OrderManagement.Application.Features.Orders.CancelOrder;
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
    public sealed class CancelOrderUseCaseTests
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
        public async Task ExecuteAsync_WithOpenOrder_ShouldCancelAndCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Order order = orderCommandRepository.Seed(CreateOrder());

            Result result = await useCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(OrderStatus.Cancelled, order.Status);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithInventoryApplied_ShouldRestoreArticleStockExactlyOnce()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = article.UpdateStock(-3);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, article.Stock);
            Assert.IsFalse(order.IsInventoryApplied, "Cancel must mark the inventory effect as reversed.");
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMultipleDifferentArticles_ShouldRestoreEachArticleStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

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

            Result result = await useCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(10, articleA.Stock);
            Assert.AreEqual(10, articleB.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithInventoryNotApplied_ShouldNotChangeStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, article.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WhenOrderAlreadyCancelled_ShouldFailAndNotDoubleRestoreStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = article.UpdateStock(-3);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result first = await useCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));
            Result second = await useCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));

            Assert.IsTrue(first.IsSuccess, first.Error);
            Assert.IsFalse(second.IsSuccess);
            Assert.AreEqual(5, article.Stock, "Stock must not be restored a second time.");
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WhenOrderIsCompleted_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Order order = CreateOrder();
            _ = order.AddLine(new ArticleId(1), "Widget", Money.From(10m, "CHF").EnsureValue(), 1);
            order.MarkInventoryApplied().EnsureSuccess();
            order.StartProcessing(FixedNow.UtcDateTime).EnsureSuccess();
            order.Complete(FixedNow.UtcDateTime).EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_DeletingAnAlreadyCancelledOrder_ShouldNotIncreaseStockAgain()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var cancelUseCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));
            var deleteUseCase = new DeleteOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = article.UpdateStock(-3);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result cancelResult = await cancelUseCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));
            Assert.IsTrue(cancelResult.IsSuccess, cancelResult.Error);
            Assert.AreEqual(5, article.Stock);

            Result deleteResult = await deleteUseCase.ExecuteAsync(new DeleteOrderCommand(order.Id.Value));

            Assert.IsTrue(deleteResult.IsSuccess, deleteResult.Error);
            Assert.AreEqual(5, article.Stock, "Deleting an already-cancelled order must not restore stock a second time.");
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMissingArticle_ShouldFailWithoutCommitting()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Article article = Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue();
            TestIdAssigner.Assign(article, new ArticleId(42));

            Order order = CreateOrder();
            _ = order.AddLine(article.Id, "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            order.MarkInventoryApplied().EnsureSuccess();
            _ = orderCommandRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCommitFailure_ShouldReturnFailure()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork { FailureMessage = "Concurrency conflict." };
            var useCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Order order = orderCommandRepository.Seed(CreateOrder());

            Result result = await useCase.ExecuteAsync(new CancelOrderCommand(order.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Concurrency conflict.", result.Error);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CancelOrderUseCase(orderCommandRepository, articleCommandRepository, unitOfWork, new FakeTimeProvider(FixedNow));

            Result result = await useCase.ExecuteAsync(new CancelOrderCommand(999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
