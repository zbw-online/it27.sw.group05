using Microsoft.EntityFrameworkCore;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence
{
    [TestClass]
    public sealed class StockConcurrencyTests : IntegrationTestBase
    {
        [TestMethod]
        public async Task CommitAsync_WithSimultaneousStockUpdates_ShouldRejectSecondWriterAndNotOversell()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, stock: 5);
            ArticleId articleId = article.Id;
            InfrastructureTestDataFactory.ClearTracker(DbContext);

            string connectionString = DbContext.Database.GetConnectionString()!;
            await using OrderManagementDbContext secondContext = CreateSecondContext(connectionString);

            Article firstReader = await DbContext.Articles.SingleAsync(a => a.Id == articleId);
            Article secondReader = await secondContext.Articles.SingleAsync(a => a.Id == articleId);

            _ = firstReader.UpdateStock(-3);
            _ = secondReader.UpdateStock(-3);

            var firstUnitOfWork = new UnitOfWork(DbContext);
            var secondUnitOfWork = new UnitOfWork(secondContext);

            Result firstResult = await firstUnitOfWork.CommitAsync();
            Result secondResult = await secondUnitOfWork.CommitAsync();

            Assert.IsTrue(firstResult.IsSuccess, firstResult.Error);
            Assert.IsFalse(secondResult.IsSuccess);
            Assert.AreEqual(
                "Der Lagerbestand wurde zwischenzeitlich geändert. Bitte laden Sie die Artikel erneut und prüfen Sie die Mengen.",
                secondResult.Error);

            InfrastructureTestDataFactory.ClearTracker(DbContext);
            Article persisted = await DbContext.Articles.AsNoTracking().SingleAsync(a => a.Id == articleId);
            Assert.AreEqual(2, persisted.Stock);
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
