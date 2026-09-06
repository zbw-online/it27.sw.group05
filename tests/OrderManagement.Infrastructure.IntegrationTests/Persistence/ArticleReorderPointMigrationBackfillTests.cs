using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using OrderManagement.Infrastructure.Persistence;
using OrderManagement.TestSupport;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence
{
    [TestClass]
    public sealed class ArticleReorderPointMigrationBackfillTests
    {
        private const string PreReorderPointMigrationId = "20260830212229_AddInventoryReconciliationTracking";

        [TestMethod]
        public async Task Migrate_WithPreExistingArticle_ShouldBackfillReorderPointWithTwenty()
        {
            string databaseName = TestDatabaseName.Create("OrderManagement_ReorderPointMigration");
            string connectionString = TestDatabaseName.BuildScopedConnectionString(
                AssemblySetup.MasterConnectionString, databaseName);

            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(OrderManagementDbContext).Assembly.FullName))
                .Options;

            await using var dbContext = new OrderManagementDbContext(options);

            try
            {
                IMigrator migrator = dbContext.GetService<IMigrator>();

                await migrator.MigrateAsync(PreReorderPointMigrationId);

                _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO [ArticleGroups] ([Name], [Status])
                    VALUES ('Legacy Group', 1);
                    """);

                _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO [Articles]
                        ([ArticleNumber], [Name], [PriceAmount], [PriceCurrency], [ArticleGroupId], [Stock], [VatRate], [Status])
                    SELECT 'ART-LEGACY-001', 'Legacy Article', 9.99, 'CHF', [ArticleGroupId], 3, 7.70, 1
                    FROM [ArticleGroups] WHERE [Name] = 'Legacy Group';
                    """);

                await migrator.MigrateAsync();

                int reorderPoint = await dbContext.Database.SqlQueryRaw<int>(
                    "SELECT [ReorderPoint] AS [Value] FROM [Articles] WHERE [ArticleNumber] = 'ART-LEGACY-001'")
                    .SingleAsync();

                Assert.AreEqual(20, reorderPoint);
            }
            finally
            {
                _ = await dbContext.Database.EnsureDeletedAsync();
            }
        }
    }
}
