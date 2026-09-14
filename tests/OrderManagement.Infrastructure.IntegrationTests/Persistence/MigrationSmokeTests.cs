using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence
{
    [TestClass]
    public sealed class MigrationSmokeTests : IntegrationTestBase
    {
        [TestMethod]
        public async Task Database_MigrateAsync_ShouldCreateSchemaSuccessfully()
        {
            bool canConnect = await DbContext.Database.CanConnectAsync();

            Assert.IsTrue(canConnect);
        }

        [TestMethod]
        public async Task Database_ShouldContainExpectedCoreTables()
        {
            string[] expectedTables =
            [
                "Customers",
                "CustomerAddresses",
                "ArticleGroups",
                "Articles",
                "Orders",
                "OrderLines"
            ];

            List<string> tableNames = await DbContext.Database
                .SqlQueryRaw<string>("SELECT [Name] = name FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo')")
                .ToListAsync();

            foreach (string expectedTable in expectedTables)
            {
                Assert.IsTrue(
                    tableNames.Contains(expectedTable),
                    $"Expected table dbo.{expectedTable} was not created by the migration.");
            }
        }
    }
}
