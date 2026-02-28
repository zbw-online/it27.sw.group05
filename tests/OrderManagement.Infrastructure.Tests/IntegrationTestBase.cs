using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
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
            // Note: Articles can use ExecuteDeleteAsync (not temporal)
            // ArticleGroups must use RemoveRange (temporal table)

            // Clear Articles (not temporal - can use ExecuteDeleteAsync)
            _ = await DbContext.Articles.ExecuteDeleteAsync();

            // Load and remove ArticleGroups (temporal table)
            List<ArticleGroup> articleGroups = await DbContext.ArticleGroups.ToListAsync();
            DbContext.ArticleGroups.RemoveRange(articleGroups);

            // Save deletions for temporal tables
            _ = await DbContext.SaveChangesAsync();

            // TODO: Add Customers and Orders when implemented
            // Customers is temporal - use RemoveRange:
            // var customers = await DbContext.Customers.ToListAsync();
            // DbContext.Customers.RemoveRange(customers);
            // await DbContext.SaveChangesAsync();
        }
    }
}
