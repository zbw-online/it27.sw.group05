using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Features.Orders.CancelOrder;
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
    public sealed class CancelOrderUseCaseIntegrationTests : IntegrationTestBase
    {
        private OrderCommandRepository _orderCommandRepository = default!;
        private ArticleCommandRepository _articleCommandRepository = default!;
        private UnitOfWork _unitOfWork = default!;
        private CancelOrderUseCase _useCase = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _orderCommandRepository = new OrderCommandRepository(DbContext);
            _articleCommandRepository = new ArticleCommandRepository(DbContext);
            _unitOfWork = new UnitOfWork(DbContext);
            _useCase = new CancelOrderUseCase(_orderCommandRepository, _articleCommandRepository, _unitOfWork, TimeProvider.System);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task ExecuteAsync_WithAppliedInventory_ShouldCancelAndRestoreStockInSameTransaction()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithAppliedInventoryAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            Result result = await _useCase.ExecuteAsync(new CancelOrderCommand(orderId.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            Order persistedOrder = await DbContext.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);

            Assert.AreEqual(OrderStatus.Cancelled, persistedOrder.Status);
            Assert.IsFalse(persistedOrder.IsInventoryApplied);
            Assert.AreEqual(10, persistedArticle.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMultipleArticleLines_ShouldRestoreEachArticleStock()
        {
            Article articleA = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Article articleB = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);

            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext);
            order.AddLine(articleA.Id, articleA.Name, articleA.Price, quantity: 2).EnsureSuccess();
            order.AddLine(articleB.Id, articleB.Name, articleB.Price, quantity: 4).EnsureSuccess();
            articleA.UpdateStock(-2).EnsureSuccess();
            articleB.UpdateStock(-4).EnsureSuccess();
            order.MarkInventoryApplied().EnsureSuccess();
            _ = await DbContext.SaveChangesAsync();

            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            Result result = await _useCase.ExecuteAsync(new CancelOrderCommand(orderId.Value));
            Assert.IsTrue(result.IsSuccess, result.Error);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            Article persistedA = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == articleA.Id);
            Article persistedB = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == articleB.Id);

            Assert.AreEqual(10, persistedA.Stock);
            Assert.AreEqual(10, persistedB.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WhenCalledTwiceInParallel_ShouldRestoreStockExactlyOnce()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithAppliedInventoryAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            string connectionString = DbContext.Database.GetConnectionString()!;
            await using OrderManagementDbContext secondContext = CreateSecondContext(connectionString);
            var secondOrderRepository = new OrderCommandRepository(secondContext);
            var secondArticleRepository = new ArticleCommandRepository(secondContext);
            var secondUnitOfWork = new UnitOfWork(secondContext);
            var secondUseCase = new CancelOrderUseCase(secondOrderRepository, secondArticleRepository, secondUnitOfWork, TimeProvider.System);

            // Both use cases load the same still-open order independently before either commits,
            // simulating two near-simultaneous cancellation requests for the same order.
            Order firstTracked = (await _orderCommandRepository.GetByIdAsync(orderId))!;
            Order secondTracked = (await secondOrderRepository.GetByIdAsync(orderId))!;
            Assert.IsNotNull(firstTracked);
            Assert.IsNotNull(secondTracked);

            // The first commit changes the order's own RowVersion concurrency token (see
            // OrderConfiguration); the second use case is still holding the order it loaded before
            // that commit, so its own commit must be rejected as stale - this is what prevents a
            // double refund even though the two use cases never touch the same tracked Article.
            Result firstResult = await _useCase.ExecuteAsync(new CancelOrderCommand(orderId.Value));
            Result secondResult = await secondUseCase.ExecuteAsync(new CancelOrderCommand(orderId.Value));

            Assert.IsTrue(firstResult.IsSuccess, firstResult.Error);
            Assert.IsFalse(secondResult.IsSuccess, "A second, concurrent cancellation of the same order must not also succeed.");

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);
            Order persistedOrder = await DbContext.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);

            Assert.AreEqual(10, persistedArticle.Stock, "Stock must be restored exactly once, regardless of how many concurrent cancellation attempts ran.");
            Assert.AreEqual(OrderStatus.Cancelled, persistedOrder.Status);
        }

        [TestMethod]
        public async Task ExecuteAsync_DeletingAnAlreadyCancelledOrder_ShouldNotIncreaseStockAgain()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 10);
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithAppliedInventoryAsync(DbContext, article: article, quantity: 3);
            OrderId orderId = order.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            Result cancelResult = await _useCase.ExecuteAsync(new CancelOrderCommand(orderId.Value));
            Assert.IsTrue(cancelResult.IsSuccess, cancelResult.Error);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            var deleteUseCase = new DeleteOrderUseCase(_orderCommandRepository, _articleCommandRepository, _unitOfWork);
            Result deleteResult = await deleteUseCase.ExecuteAsync(new DeleteOrderCommand(orderId.Value));

            Assert.IsTrue(deleteResult.IsSuccess, deleteResult.Error);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            Article persistedArticle = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == article.Id);
            Assert.AreEqual(10, persistedArticle.Stock, "Deleting an already-cancelled order must not restore stock a second time.");
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMissingOrder_ShouldFail()
        {
            Result result = await _useCase.ExecuteAsync(new CancelOrderCommand(999_999));

            Assert.IsFalse(result.IsSuccess);
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
