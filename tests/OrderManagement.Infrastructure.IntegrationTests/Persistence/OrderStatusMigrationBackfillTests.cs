using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using OrderManagement.Infrastructure.Persistence;
using OrderManagement.TestSupport;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence
{
    [TestClass]
    public sealed class OrderStatusMigrationBackfillTests
    {
        private const string PreStatusMigrationId = "20260901160831_AddArticleReorderPoint";

        [TestMethod]
        public async Task Migrate_WithPreExistingOrders_ShouldBackfillSafeInitialStatusFromInventoryFlag()
        {
            string databaseName = TestDatabaseName.Create("OrderManagement_StatusMigration");
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

                await migrator.MigrateAsync(PreStatusMigrationId);

                var legacyOrderDate = new DateTime(2026, 5, 12, 9, 30, 0, DateTimeKind.Utc);

                _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO [Customers] ([CustomerNumber], [LastName], [SurName], [Email], [Website])
                    VALUES ('CU00001', 'Doe', 'Jane', 'jane@example.com', NULL);
                    """);

                _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO [Orders]
                        ([OrderNumber], [OrderDate], [CustomerId], [DeliveryDate],
                         [BillingStreet], [BillingHouseNumber], [BillingPostalCode], [BillingCity], [BillingCountryCode], [BillingAddressSource],
                         [DeliveryStreet], [DeliveryHouseNumber], [DeliveryPostalCode], [DeliveryCity], [DeliveryCountryCode], [DeliveryAddressSource],
                         [TotalAmount], [TotalCurrency], [IsInventoryApplied])
                    SELECT 'ORD-LEGACY-901', {legacyOrderDate}, [CustomerId], CAST({legacyOrderDate} AS date),
                           'Legacy Street', '1', '9000', 'St. Gallen', 'CH', 'Automatic',
                           'Legacy Street', '1', '9000', 'St. Gallen', 'CH', 'Automatic',
                           19.98, 'CHF', 1
                    FROM [Customers] WHERE [CustomerNumber] = 'CU00001';
                    """);

                _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO [Orders]
                        ([OrderNumber], [OrderDate], [CustomerId], [DeliveryDate],
                         [BillingStreet], [BillingHouseNumber], [BillingPostalCode], [BillingCity], [BillingCountryCode], [BillingAddressSource],
                         [DeliveryStreet], [DeliveryHouseNumber], [DeliveryPostalCode], [DeliveryCity], [DeliveryCountryCode], [DeliveryAddressSource],
                         [TotalAmount], [TotalCurrency], [IsInventoryApplied])
                    SELECT 'ORD-LEGACY-902', {legacyOrderDate}, [CustomerId], CAST({legacyOrderDate} AS date),
                           'Legacy Street', '2', '9000', 'St. Gallen', 'CH', 'Automatic',
                           'Legacy Street', '2', '9000', 'St. Gallen', 'CH', 'Automatic',
                           9.99, 'CHF', 0
                    FROM [Customers] WHERE [CustomerNumber] = 'CU00001';
                    """);

                await migrator.MigrateAsync();

                string appliedStatus = await dbContext.Database.SqlQueryRaw<string>(
                    "SELECT [Status] AS [Value] FROM [Orders] WHERE [OrderNumber] = 'ORD-LEGACY-901'")
                    .SingleAsync();
                DateTime? appliedStatusChangedAt = await dbContext.Database.SqlQueryRaw<DateTime?>(
                    "SELECT [StatusChangedAtUtc] AS [Value] FROM [Orders] WHERE [OrderNumber] = 'ORD-LEGACY-901'")
                    .SingleAsync();

                string notAppliedStatus = await dbContext.Database.SqlQueryRaw<string>(
                    "SELECT [Status] AS [Value] FROM [Orders] WHERE [OrderNumber] = 'ORD-LEGACY-902'")
                    .SingleAsync();
                DateTime? notAppliedStatusChangedAt = await dbContext.Database.SqlQueryRaw<DateTime?>(
                    "SELECT [StatusChangedAtUtc] AS [Value] FROM [Orders] WHERE [OrderNumber] = 'ORD-LEGACY-902'")
                    .SingleAsync();

                Assert.AreEqual("Completed", appliedStatus);
                Assert.AreEqual(legacyOrderDate, appliedStatusChangedAt);

                Assert.AreEqual("Open", notAppliedStatus);
                Assert.IsNull(notAppliedStatusChangedAt);
            }
            finally
            {
                _ = await dbContext.Database.EnsureDeletedAsync();
            }
        }
    }
}
