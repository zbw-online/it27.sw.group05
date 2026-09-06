using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using OrderManagement.Infrastructure.Persistence;
using OrderManagement.TestSupport;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence
{
    [TestClass]
    public sealed class LegacyOrderMigrationBackfillTests
    {
        private const string InitialCreateMigrationId = "20260606153021_InitialCreate";

        [TestMethod]
        public async Task Migrate_WithPreExistingLegacyOrder_ShouldBackfillBillingAddressAndDeliveryDateFromOrderDate()
        {
            string databaseName = TestDatabaseName.Create("OrderManagement_LegacyMigration");
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

                await migrator.MigrateAsync(InitialCreateMigrationId);

                var legacyOrderDate = new DateTime(2026, 5, 12, 9, 30, 0, DateTimeKind.Utc);

                _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO [Customers] ([CustomerNumber], [LastName], [SurName], [Email], [Website])
                    VALUES ('CU00001', 'Doe', 'Jane', 'jane@example.com', NULL);
                    """);

                _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO [Orders]
                        ([OrderNumber], [OrderDate], [CustomerId], [DeliveryStreet], [DeliveryHouseNumber],
                         [DeliveryPostalCode], [DeliveryCity], [DeliveryCountryCode], [TotalAmount], [TotalCurrency])
                    SELECT 'ORD-LEGACY-001', {legacyOrderDate}, [CustomerId], 'Legacy Street', '5',
                           '9000', 'St. Gallen', 'CH', 19.98, 'CHF'
                    FROM [Customers] WHERE [CustomerNumber] = 'CU00001';
                    """);

                await migrator.MigrateAsync();

                string billingStreet = await dbContext.Database.SqlQueryRaw<string>(
                    "SELECT [BillingStreet] AS [Value] FROM [Orders] WHERE [OrderNumber] = 'ORD-LEGACY-001'")
                    .SingleAsync();

                DateOnly deliveryDate = await dbContext.Database.SqlQueryRaw<DateOnly>(
                    "SELECT [DeliveryDate] AS [Value] FROM [Orders] WHERE [OrderNumber] = 'ORD-LEGACY-001'")
                    .SingleAsync();

                string billingAddressSource = await dbContext.Database.SqlQueryRaw<string>(
                    "SELECT [BillingAddressSource] AS [Value] FROM [Orders] WHERE [OrderNumber] = 'ORD-LEGACY-001'")
                    .SingleAsync();

                Assert.AreEqual("Legacy Street", billingStreet);
                Assert.AreEqual(DateOnly.FromDateTime(legacyOrderDate), deliveryDate);
                Assert.AreEqual("Automatic", billingAddressSource);
            }
            finally
            {
                _ = await dbContext.Database.EnsureDeletedAsync();
            }
        }
    }
}
