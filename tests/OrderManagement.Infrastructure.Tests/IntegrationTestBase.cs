using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
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

            // Clear in order respecting foreign keys

            // Clear OrderLines first (if they exist as a separate table)
            List<OrderLine> orderLines = await DbContext.OrderLines.ToListAsync();
            DbContext.OrderLines.RemoveRange(orderLines);
            _ = await DbContext.SaveChangesAsync();

            // Clear Orders (temporal - must use RemoveRange)
            List<Order> orders = await DbContext.Orders.ToListAsync();
            DbContext.Orders.RemoveRange(orders);
            _ = await DbContext.SaveChangesAsync();

            // Clear CustomerAddresses
            List<CustomerAddress> addresses = await DbContext.CustomerAddresses.ToListAsync();
            DbContext.CustomerAddresses.RemoveRange(addresses);
            _ = await DbContext.SaveChangesAsync();

            // Clear Customers (temporal - must use RemoveRange)
            List<Customer> customers = await DbContext.Customers.ToListAsync();
            DbContext.Customers.RemoveRange(customers);
            _ = await DbContext.SaveChangesAsync();

            // Clear Articles (not temporal - can use ExecuteDeleteAsync)
            _ = await DbContext.Articles.ExecuteDeleteAsync();

            // Load and remove ArticleGroups (temporal table)
            List<ArticleGroup> articleGroups = await DbContext.ArticleGroups.ToListAsync();
            DbContext.ArticleGroups.RemoveRange(articleGroups);
            _ = await DbContext.SaveChangesAsync();
        }
    }
}
