using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using OrderManagement.Domain.Catalog;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Tests.Persistence
{
    [TestClass]
    public sealed class OrderManagementDbContextTests
    {
        [TestMethod]
        public async Task CanCreateAndQueryArticleGroupWithSqliteInMemory()
        {
            // Arrange - single in-memory sqlite connection so multiple DbContext instances share the same DB
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlite(connection)
                .Options;

            // Create schema and insert using first context
            await using (var context = new OrderManagementDbContext(options))
            {
                _ = await context.Database.EnsureCreatedAsync();

                SharedKernel.Primitives.Result<ArticleGroup> groupResult = ArticleGroup.Create(1, "TestGroup");
                Assert.IsTrue(groupResult.IsSuccess);

                if (groupResult.Value is not null)
                {
                    _ = context.ArticleGroups.Add(groupResult.Value);
                }
                else
                {
                    Assert.Fail("ArticleGroup.Create returned a null Value.");
                }

                _ = await context.SaveChangesAsync();
            }

            // Verify with a new context instance (same open connection)
            await using (var verifyContext = new OrderManagementDbContext(options))
            {
                bool exists = await verifyContext.ArticleGroups.AnyAsync(g => g.Name == "TestGroup");
                Assert.IsTrue(exists);
            }

            await connection.CloseAsync();
        }
    }
}
