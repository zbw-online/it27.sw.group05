using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Infrastructure.Persistence;

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

        private static string CreateConnectionString(string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(AssemblySetup.MasterConnectionString)
            {
                InitialCatalog = databaseName,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            return builder.ConnectionString;
        }

        private string CreateDatabaseName()
        {
            string testName = TestContext.TestName ?? "UnknownTest";
            string safeTestName = new([.. testName.Where(char.IsLetterOrDigit).Take(45)]);
            return $"OrderManagement_Test_{safeTestName}_{Guid.NewGuid():N}";
        }
    }
}
