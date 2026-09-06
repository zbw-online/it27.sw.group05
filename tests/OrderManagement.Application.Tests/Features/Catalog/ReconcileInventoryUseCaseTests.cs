using OrderManagement.Application.Features.Catalog.ReconcileInventory;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class ReconcileInventoryUseCaseTests
    {
        private static Order UnreconciledOrder(string orderNumber, ArticleId articleId, string articleName, Money price, int quantity)
        {
            Order order = Order.Create(
                orderNumber,
                new CustomerId(1),
                new DateOnly(2026, 9, 1),
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic).EnsureValue();

            order.AddLine(articleId, articleName, price, quantity).EnsureSuccess();
            return order;
        }

        [TestMethod]
        public async Task ExecuteAsync_WithNoUnreconciledOrders_ShouldReturnEmptyReportAndNotCommit()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new ReconcileInventoryUseCase(orderQueryRepository, orderCommandRepository, articleCommandRepository, unitOfWork);

            Result<ReconciliationReportDto> result = await useCase.ExecuteAsync(new ReconcileInventoryCommand(Apply: true));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(0, result.Value!.AffectedOrderNumbers.Count);
            Assert.IsFalse(result.Value.WasApplied);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_DryRun_ShouldReportWithoutChangingStockOrOrders()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new ReconcileInventoryUseCase(orderQueryRepository, orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 20).EnsureValue());
            Order order = orderQueryRepository.Seed(
                UnreconciledOrder("ORD-2025-001", article.Id, article.Name, article.Price, 5));

            Result<ReconciliationReportDto> result = await useCase.ExecuteAsync(new ReconcileInventoryCommand(Apply: false));

            Assert.IsTrue(result.IsSuccess, result.Error);
            ReconciliationReportDto report = result.Value!;
            Assert.IsFalse(report.WasApplied);
            CollectionAssert.Contains(report.AffectedOrderNumbers.ToList(), "ORD-2025-001");

            ReconciliationArticleImpactDto impact = report.ArticleImpacts.Single();
            Assert.AreEqual(article.Id.Value, impact.ArticleId);
            Assert.AreEqual(20, impact.CurrentStock);
            Assert.AreEqual(5, impact.QuantityToDeduct);
            Assert.AreEqual(15, impact.ResultingStock);
            Assert.IsFalse(impact.HasInsufficientStock);

            Assert.AreEqual(20, article.Stock);
            Assert.IsFalse(order.IsInventoryApplied);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_Apply_WithSufficientStock_ShouldDeductStockAndMarkOrdersApplied()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new ReconcileInventoryUseCase(orderQueryRepository, orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 20).EnsureValue());

            Order firstOrder = UnreconciledOrder("ORD-2025-001", article.Id, article.Name, article.Price, 5);
            Order secondOrder = UnreconciledOrder("ORD-2025-002", article.Id, article.Name, article.Price, 3);
            _ = orderQueryRepository.Seed(firstOrder);
            _ = orderQueryRepository.Seed(secondOrder);
            _ = orderCommandRepository.Seed(firstOrder);
            _ = orderCommandRepository.Seed(secondOrder);

            Result<ReconciliationReportDto> result = await useCase.ExecuteAsync(new ReconcileInventoryCommand(Apply: true));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(result.Value!.WasApplied);
            Assert.AreEqual(12, article.Stock);
            Assert.IsTrue(firstOrder.IsInventoryApplied);
            Assert.IsTrue(secondOrder.IsInventoryApplied);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_Apply_SecondRun_ShouldBeNoOp()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new ReconcileInventoryUseCase(orderQueryRepository, orderCommandRepository, articleCommandRepository, unitOfWork);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 20).EnsureValue());
            Order order = UnreconciledOrder("ORD-2025-001", article.Id, article.Name, article.Price, 5);
            _ = orderQueryRepository.Seed(order);
            _ = orderCommandRepository.Seed(order);

            Result<ReconciliationReportDto> firstRun = await useCase.ExecuteAsync(new ReconcileInventoryCommand(Apply: true));
            Assert.IsTrue(firstRun.IsSuccess, firstRun.Error);
            Assert.AreEqual(15, article.Stock);
            Assert.AreEqual(1, unitOfWork.CommitCount);

            // FakeOrderQueryRepository reflects live object state, so the order that was just
            // marked applied is no longer returned by GetUnreconciledOrdersAsync on the next run.
            Result<ReconciliationReportDto> secondRun = await useCase.ExecuteAsync(new ReconcileInventoryCommand(Apply: true));

            Assert.IsTrue(secondRun.IsSuccess, secondRun.Error);
            Assert.IsFalse(secondRun.Value!.WasApplied);
            Assert.AreEqual(15, article.Stock);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_Apply_WithInsufficientStock_ShouldRejectFullyAndChangeNothing()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var orderCommandRepository = new FakeOrderCommandRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new ReconcileInventoryUseCase(orderQueryRepository, orderCommandRepository, articleCommandRepository, unitOfWork);

            Article shortArticle = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 2).EnsureValue());
            Article healthyArticle = articleCommandRepository.Seed(
                Article.Create("ART-002", "Gadget", 5.00m, "CHF", new ArticleGroupId(1), stock: 20).EnsureValue());

            Order shortOrder = UnreconciledOrder("ORD-2025-001", shortArticle.Id, shortArticle.Name, shortArticle.Price, 5);
            Order healthyOrder = UnreconciledOrder("ORD-2025-002", healthyArticle.Id, healthyArticle.Name, healthyArticle.Price, 3);
            _ = orderQueryRepository.Seed(shortOrder);
            _ = orderQueryRepository.Seed(healthyOrder);
            _ = orderCommandRepository.Seed(shortOrder);
            _ = orderCommandRepository.Seed(healthyOrder);

            Result<ReconciliationReportDto> result = await useCase.ExecuteAsync(new ReconcileInventoryCommand(Apply: true));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsFalse(result.Value!.WasApplied);
            Assert.AreEqual(1, result.Value.Conflicts.Count);

            Assert.AreEqual(2, shortArticle.Stock);
            Assert.AreEqual(20, healthyArticle.Stock);
            Assert.IsFalse(shortOrder.IsInventoryApplied);
            Assert.IsFalse(healthyOrder.IsInventoryApplied);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
