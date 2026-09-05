using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Infrastructure.Persistence;
using OrderManagement.TestSupport;

namespace OrderManagement.Infrastructure.IntegrationTests
{
    public abstract class IntegrationTestBase
    {
        protected OrderManagementDbContext DbContext { get; private set; } = default!;

        public TestContext TestContext { get; set; } = default!;

        [TestInitialize]
        public async Task TestInitializeAsync()
        {
            string databaseName = CreateDatabaseName();
            string connectionString = CreateConnectionString(databaseName);

            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(OrderManagementDbContext).Assembly.FullName))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .Options;

            DbContext = new OrderManagementDbContext(options);

            await DbContext.Database.MigrateAsync();
            await OnDatabaseInitializedAsync();
        }

        [TestCleanup]
        public async Task TestCleanupAsync()
        {
            if (DbContext is null)
            {
                return;
            }

            _ = await DbContext.Database.EnsureDeletedAsync();
            await DbContext.DisposeAsync();
        }

        protected virtual Task OnDatabaseInitializedAsync() => Task.CompletedTask;

        private static string CreateConnectionString(string databaseName) =>
            TestDatabaseName.BuildScopedConnectionString(AssemblySetup.MasterConnectionString, databaseName);

        private string CreateDatabaseName() =>
            TestDatabaseName.Create("OrderManagement_Test", TestContext.TestName);
    }
}
