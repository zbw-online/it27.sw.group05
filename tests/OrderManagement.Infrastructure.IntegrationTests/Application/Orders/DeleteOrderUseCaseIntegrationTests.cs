using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithLineAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;
            OrderLineId lineId = order.Lines.Single().Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

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
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithLineAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            string connectionString = DbContext.Database.GetConnectionString()!;
            await using OrderManagementDbContext concurrentContext = CreateSecondContext(connectionString);
            Article concurrentArticle = await concurrentContext.Articles.SingleAsync(a => a.Id == article.Id);
            _ = concurrentArticle.UpdateStock(5);
            _ = concurrentContext.Articles.Update(concurrentArticle);
            _ = await concurrentContext.SaveChangesAsync();

            Result result = await _useCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));

            Assert.IsFalse(result.IsSuccess);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            bool orderStillExists = await DbContext.Orders.AsNoTracking().AnyAsync(o => o.Id == orderId);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);

            Assert.IsTrue(orderStillExists);
            Assert.AreEqual(15, persistedArticle.Stock);
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
