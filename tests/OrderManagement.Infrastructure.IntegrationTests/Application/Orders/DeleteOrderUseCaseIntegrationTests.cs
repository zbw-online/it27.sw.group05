using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Features.Orders.DeleteOrder;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Command;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Application.Orders
{
    [TestClass]
    public sealed class DeleteOrderUseCaseIntegrationTests : IntegrationTestBase
    {
        private OrderCommandRepository _orderCommandRepository = default!;
        private ArticleCommandRepository _articleCommandRepository = default!;
        private UnitOfWork _unitOfWork = default!;
        private DeleteOrderUseCase _useCase = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _orderCommandRepository = new OrderCommandRepository(DbContext);
            _articleCommandRepository = new ArticleCommandRepository(DbContext);
            _unitOfWork = new UnitOfWork(DbContext);
            _useCase = new DeleteOrderUseCase(_orderCommandRepository, _articleCommandRepository, _unitOfWork);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task ExecuteAsync_WithExistingOrder_ShouldRemoveOrderAndLinesAndRestoreStockInSameTransaction()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithAppliedInventoryAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;
            OrderLineId lineId = order.Lines.Single().Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            Article stockAfterCreation = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);
            Assert.AreEqual(7, stockAfterCreation.Stock, "Stock should be deducted once the order's inventory has been applied.");

            Result result = await _useCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            bool orderExists = await DbContext.Orders.AsNoTracking().AnyAsync(o => o.Id == orderId);
            bool lineExists = await DbContext.OrderLines.AsNoTracking().AnyAsync(l => l.Id == lineId);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);

            Assert.IsFalse(orderExists);
            Assert.IsFalse(lineExists);
            Assert.AreEqual(10, persistedArticle.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMultipleArticleLines_ShouldRestoreEachArticleStock()
        {
            Article articleA = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Article articleB = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);

            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext);
            Result addLineAResult = order.AddLine(articleA.Id, articleA.Name, articleA.Price, quantity: 2);
            Result addLineBResult = order.AddLine(articleB.Id, articleB.Name, articleB.Price, quantity: 4);
            Assert.IsTrue(addLineAResult.IsSuccess, addLineAResult.Error);
            Assert.IsTrue(addLineBResult.IsSuccess, addLineBResult.Error);

            Result stockAResult = articleA.UpdateStock(-2);
            Result stockBResult = articleB.UpdateStock(-4);
            Assert.IsTrue(stockAResult.IsSuccess, stockAResult.Error);
            Assert.IsTrue(stockBResult.IsSuccess, stockBResult.Error);

            Result markAppliedResult = order.MarkInventoryApplied();
            Assert.IsTrue(markAppliedResult.IsSuccess, markAppliedResult.Error);

            _ = await DbContext.SaveChangesAsync();

            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            Result result = await _useCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));
            Assert.IsTrue(result.IsSuccess, result.Error);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            Article persistedA = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == articleA.Id);
            Article persistedB = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == articleB.Id);

            Assert.AreEqual(10, persistedA.Stock);
            Assert.AreEqual(10, persistedB.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithConcurrentStockChangeOnAffectedArticle_ShouldFailAndLeaveConsistentData()
        {
            // Initial stock 10, minus the order's quantity of 3 once inventory is applied -> 7 persisted.
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithAppliedInventoryAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            // Context A (the same DbContext the use case will use) loads and tracks the article now,
            // capturing its concurrency token before Context B changes the row.
            Article trackedArticle = await DbContext.Articles.SingleAsync(a => a.Id == article.Id);
            Assert.AreEqual(7, trackedArticle.Stock);

            // Context B changes the same article's stock by +5 and commits -> persisted stock becomes 12.
            string connectionString = DbContext.Database.GetConnectionString()!;
            await using OrderManagementDbContext concurrentContext = CreateSecondContext(connectionString);
            Article concurrentArticle = await concurrentContext.Articles.SingleAsync(a => a.Id == article.Id);
            _ = concurrentArticle.UpdateStock(5);
            _ = concurrentContext.Articles.Update(concurrentArticle);
            _ = await concurrentContext.SaveChangesAsync();

            // Context A attempts to delete the order and restore stock using its now-stale tracked article.
            Result result = await _useCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));

            Assert.IsFalse(result.IsSuccess);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            bool orderStillExists = await DbContext.Orders.AsNoTracking().AnyAsync(o => o.Id == orderId);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);

            Assert.IsTrue(orderStillExists);
            Assert.AreEqual(12, persistedArticle.Stock, "Only Context B's successful update should be reflected; no partial restoration may be persisted.");
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMissingOrder_ShouldFailWithoutChangingStock()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);

            Result result = await _useCase.ExecuteAsync(new DeleteOrderCommand(999_999));

            Assert.IsFalse(result.IsSuccess);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);
            Assert.AreEqual(10, persistedArticle.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithInventoryNotApplied_ShouldRemoveOrderWithoutChangingStock()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithLineAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            Result result = await _useCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            bool orderExists = await DbContext.Orders.AsNoTracking().AnyAsync(o => o.Id == orderId);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);

            Assert.IsFalse(orderExists);
            Assert.AreEqual(10, persistedArticle.Stock, "Stock was never deducted, so deletion must not restore it.");
        }

        [TestMethod]
        public async Task ExecuteAsync_WithStockOverflowDuringRestoration_ShouldFailAndLeaveOrderIntact()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithAppliedInventoryAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;

            // Simulate the article's stock having since grown so close to int.MaxValue (e.g. through
            // unrelated stock intake) that restoring this order's quantity would overflow.
            _ = await DbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.Articles SET Stock = {int.MaxValue - 1} WHERE ArticleId = {article.Id.Value}");
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            Result result = await _useCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));

            Assert.IsFalse(result.IsSuccess);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            bool orderStillExists = await DbContext.Orders.AsNoTracking().AnyAsync(o => o.Id == orderId);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);

            Assert.IsTrue(orderStillExists);
            Assert.AreEqual(int.MaxValue - 1, persistedArticle.Stock, "No partial restoration may be persisted once the overflow guard rejects the update.");
        }

        [TestMethod]
        public async Task ExecuteAsync_WhenCalledTwice_ShouldRestoreStockExactlyOnceAndReturnNotFoundOnSecondCall()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithAppliedInventoryAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            Result firstResult = await _useCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));
            Assert.IsTrue(firstResult.IsSuccess, firstResult.Error);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            Result secondResult = await _useCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));

            Assert.IsFalse(secondResult.IsSuccess);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);
            Assert.AreEqual(10, persistedArticle.Stock, "The second, no-op deletion must not restore stock again.");
        }

        [TestMethod]
        public async Task ExecuteAsync_AfterDeletion_ShouldKeepTemporalHistoryQueryable()
        {
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithLineAsync(DbContext);
            string orderNumber = order.OrderNumber.Value;
            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            Result result = await _useCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));
            Assert.IsTrue(result.IsSuccess, result.Error);

            int historyRowCount = await DbContext.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM dbo.OrdersHistory WHERE OrderNumber = {0}", orderNumber)
                .SingleAsync();

            Assert.IsTrue(historyRowCount > 0, "The temporal history table should still contain the deleted order's prior state.");
        }

        private static OrderManagementDbContext CreateSecondContext(string connectionString)
        {
            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(OrderManagementDbContext).Assembly.FullName))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .Options;

            return new OrderManagementDbContext(options);
        }
    }
}
