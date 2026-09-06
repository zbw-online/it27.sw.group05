using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Persistence.Initialization;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Initialization
{
    [TestClass]
    public sealed class DemoDataSeederTests : IntegrationTestBase
    {
        private static readonly DateOnly Today = new(2026, 9, 6);
        private static readonly DateTimeOffset Now = new(Today, TimeOnly.MinValue, TimeSpan.Zero);

        [TestMethod]
        public async Task SeedAsync_OnEmptyDatabase_CreatesExpectedDataset()
        {
            Result result = await RunInitializerAsync(seedDemoData: true);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, await DbContext.Customers.CountAsync());
            Assert.AreEqual(8, await DbContext.ArticleGroups.CountAsync());
            Assert.AreEqual(10, await DbContext.Articles.CountAsync());
            Assert.AreEqual(8, await DbContext.Orders.CountAsync());
        }

        [TestMethod]
        public async Task SeedAsync_RunTwice_CreatesNoDuplicates()
        {
            _ = await RunInitializerAsync(seedDemoData: true);
            Result second = await RunInitializerAsync(seedDemoData: true);

            Assert.IsTrue(second.IsSuccess, second.Error);
            Assert.AreEqual(5, await DbContext.Customers.CountAsync());
            Assert.AreEqual(8, await DbContext.ArticleGroups.CountAsync());
            Assert.AreEqual(10, await DbContext.Articles.CountAsync());
            Assert.AreEqual(8, await DbContext.Orders.CountAsync());
        }

        [TestMethod]
        public async Task SeedAsync_RunTwice_DoesNotDeductStockAgain()
        {
            _ = await RunInitializerAsync(seedDemoData: true);
            int stockAfterFirstRun = await StockOfAsync("ART-00001");

            _ = await RunInitializerAsync(seedDemoData: true);
            int stockAfterSecondRun = await StockOfAsync("ART-00001");

            Assert.AreEqual(stockAfterFirstRun, stockAfterSecondRun);
        }

        [TestMethod]
        public async Task SeedAsync_DoesNotTouchUnrelatedExistingRecords()
        {
            Customer unrelated = Customer.Create("CU09999", "Bestand", "Unabhaengig", "unabhaengig@example.ch", null).EnsureValue();
            _ = DbContext.Customers.Add(unrelated);
            _ = await DbContext.SaveChangesAsync();

            Result result = await RunInitializerAsync(seedDemoData: true);

            Assert.IsTrue(result.IsSuccess, result.Error);

            Customer? stillPresent = await DbContext.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.LastName == "Bestand" && c.SurName == "Unabhaengig");

            Assert.IsNotNull(stillPresent);
            Assert.AreEqual(6, await DbContext.Customers.CountAsync());
        }

        [TestMethod]
        public async Task SeedAsync_WithConflictingCustomerNumber_FailsClearly()
        {
            Customer conflicting = Customer.Create("CU00001", "Falscher", "Name", "falsch@example.ch", null).EnsureValue();
            _ = DbContext.Customers.Add(conflicting);
            _ = await DbContext.SaveChangesAsync();

            Result result = await RunInitializerAsync(seedDemoData: true);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Error!.Contains("CU00001", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task SeedAsync_WithConflictingArticleNumber_RollsBackWithoutPartialData()
        {
            // The conflict is only discovered after customers and categories were already
            // added within the same seeding transaction - this proves the whole run rolls back,
            // not just the offending article.
            ArticleGroup unrelatedGroup = ArticleGroup.Create("Fremdkategorie").EnsureValue();
            _ = DbContext.ArticleGroups.Add(unrelatedGroup);
            _ = await DbContext.SaveChangesAsync();

            Article conflicting = Article.Create(
                "ART-00001", "Falscher Artikelname", 1.00m, "CHF", unrelatedGroup.Id, stock: 1, reorderPoint: 1).EnsureValue();
            _ = DbContext.Articles.Add(conflicting);
            _ = await DbContext.SaveChangesAsync();

            Result result = await RunInitializerAsync(seedDemoData: true);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, await DbContext.Customers.CountAsync());
            Assert.AreEqual(1, await DbContext.ArticleGroups.CountAsync());
            Assert.AreEqual(1, await DbContext.Articles.CountAsync());
            Assert.AreEqual(0, await DbContext.Orders.CountAsync());
        }

        [TestMethod]
        public async Task SeedAsync_ClassifiesHistoricalCurrentAndFutureAddressesCorrectly()
        {
            Result result = await RunInitializerAsync(seedDemoData: true);
            Assert.IsTrue(result.IsSuccess, result.Error);

            Customer customer = await DbContext.Customers
                .Include(c => c.Addresses)
                .AsNoTracking()
                .SingleAsync(c => c.CustomerNumber == Domain.Customers.ValueObjects.CustomerNumber.Create("CU00001").EnsureValue());

            CustomerAddress? historical = customer.AddressAt(Today.AddYears(-1));
            CustomerAddress? current = customer.AddressAt(Today);
            CustomerAddress? future = customer.AddressAt(Today.AddMonths(4));

            Assert.IsNotNull(historical);
            Assert.AreEqual(Today.AddYears(-2), historical.ValidFrom);

            Assert.IsNotNull(current);
            Assert.AreEqual(Today.AddMonths(-6), current.ValidFrom);

            Assert.IsNotNull(future);
            Assert.AreEqual(Today.AddMonths(3), future.ValidFrom);
            Assert.IsNull(future.ValidTo);
        }

        [TestMethod]
        public async Task SeedAsync_OrdersAndStockRemainConsistent()
        {
            Result result = await RunInitializerAsync(seedDemoData: true);
            Assert.IsTrue(result.IsSuccess, result.Error);

            Assert.AreEqual(13, await StockOfAsync("ART-00001"));
            Assert.AreEqual(3, await StockOfAsync("ART-00002"));

            List<Order> orders = await DbContext.Orders.Include(o => o.Lines).AsNoTracking().ToListAsync();
            foreach (Order order in orders)
            {
                decimal expectedTotal = order.Lines.Sum(l => l.LineTotal.Amount);
                Assert.AreEqual(expectedTotal, order.Total.Amount);
                Assert.IsTrue(order.IsInventoryApplied);
            }

            List<Article> articles = await DbContext.Articles.AsNoTracking().ToListAsync();
            Assert.IsTrue(articles.All(a => a.Stock >= 0));
        }

        [TestMethod]
        public async Task Initialize_WithSeedDemoDataDisabled_MigratesSchemaButSeedsNoDemoRecords()
        {
            Result result = await RunInitializerAsync(seedDemoData: false);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(await DbContext.Database.CanConnectAsync());
            Assert.AreEqual(0, await DbContext.Customers.CountAsync());
            Assert.AreEqual(0, await DbContext.Articles.CountAsync());
            Assert.AreEqual(0, await DbContext.Orders.CountAsync());
        }

        private async Task<int> StockOfAsync(string articleNumber)
        {
            Article article = await DbContext.Articles
                .AsNoTracking()
                .SingleAsync(a => a.ArticleNumber.Value == articleNumber);

            return article.Stock;
        }

        private Task<Result> RunInitializerAsync(bool seedDemoData)
        {
            IOptions<DatabaseInitializationOptions> options = Options.Create(new DatabaseInitializationOptions { SeedDemoData = seedDemoData });
            var timeProvider = new FakeTimeProvider(Now);
            var seeder = new DemoDataSeeder(DbContext, timeProvider);
            var initializer = new DatabaseInitializer(DbContext, seeder, options);

            return initializer.InitializeAsync();
        }
    }
}
