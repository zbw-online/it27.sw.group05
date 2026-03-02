using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Infrastructure.Persistence;

using Testcontainers.MsSql;

namespace OrderManagement.Infrastructure.Tests
{
    [TestClass]
    public static class AssemblySetup
    {
        internal static MsSqlContainer? MsSqlContainer { get; private set; }
        internal static string ConnectionString { get; private set; } = default!;

        // IMPORTANT: migrate only to schema-complete, skip SeedTestData
        private const string TargetMigration = "20260228155851_CompleteOrderManagementDb";

        [AssemblyInitialize]
        public static async Task AssemblyInitialize(TestContext _)
        {
            MsSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Test@1234!")
                .Build();

            await MsSqlContainer.StartAsync();

            var csb = new SqlConnectionStringBuilder(MsSqlContainer.GetConnectionString())
            {
                InitialCatalog = "OrderManagement_Tests"
            };

            ConnectionString = csb.ConnectionString;

            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            await using var setupContext = new OrderManagementDbContext(options);

            // Clean schema per test run (keeps suite deterministic)
#pragma warning disable IDE0058
            await setupContext.Database.EnsureDeletedAsync();
#pragma warning restore IDE0058

            // Apply only schema migrations (skip the broken seed migration)
            await setupContext.Database.MigrateAsync(TargetMigration);
        }

        [AssemblyCleanup]
        public static async Task AssemblyCleanup()
        {
            if (MsSqlContainer is not null)
                await MsSqlContainer.DisposeAsync();
        }
    }
}
