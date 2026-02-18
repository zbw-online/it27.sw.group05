using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Tests
{
    public abstract class IntegrationTestBase
    {
        protected OrderManagementDbContext? DbContext { get; private set; }

        [TestInitialize]
        public async Task TestInitialize()
        {
            // Create a new DbContext instance for each test (thread-safe)
            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlServer(AssemblySetup.ConnectionString)
                .Options;

            DbContext = new OrderManagementDbContext(options);

            await ClearDatabase();
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            if (DbContext is not null)
            {
                await DbContext.DisposeAsync();
            }
        }

        private async Task ClearDatabase()
        {
            if (DbContext is null) return;

            // Clear tables in order (respecting foreign key constraints)
            // Use ExecuteDeleteAsync to directly delete from database without loading entities
            _ = await DbContext.Articles.ExecuteDeleteAsync();
            _ = await DbContext.ArticleGroups.ExecuteDeleteAsync();
            // TODO: Add Customers and Orders when implemented
            // _ = await DbContext.Orders.ExecuteDeleteAsync();
            // _ = await DbContext.Customers.ExecuteDeleteAsync();
        }
    }
}
