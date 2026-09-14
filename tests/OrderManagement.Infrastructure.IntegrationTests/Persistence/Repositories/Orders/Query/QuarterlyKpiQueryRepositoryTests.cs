using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Domain.Catalog;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Orders.Query
{
    [TestClass]
    public sealed class QuarterlyKpiQueryRepositoryTests : IntegrationTestBase
    {
        private QuarterlyKpiQueryRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new QuarterlyKpiQueryRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task GetQuarterlyKpisLast3YearsAsync_WithEmptyDatabase_ShouldReturnRowsForAllCategoriesAndQuarters()
        {
            IReadOnlyList<QuarterlyKpiRowDto> result = await _repository.GetQuarterlyKpisLast3YearsAsync();

            Assert.AreEqual(60, result.Count, "Expected 3 years * 4 quarters * 5 KPI categories.");
            Assert.IsTrue(result.All(x => x.Value == 0m));
        }

        [TestMethod]
        public async Task GetQuarterlyKpisLast3YearsAsync_WithCurrentQuarterOrder_ShouldReturnGesamtumsatz()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, priceAmount: 12.50m);

            _ = await InfrastructureTestDataFactory.CreatePersistedOrderWithLineAsync(
                DbContext,
                article: article,
                orderDate: DateTime.UtcNow,
                quantity: 2);

            int currentYear = DateTime.UtcNow.Year;
            int currentQuarter = ((DateTime.UtcNow.Month - 1) / 3) + 1;

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<QuarterlyKpiRowDto> result = await _repository.GetQuarterlyKpisLast3YearsAsync();

            QuarterlyKpiRowDto revenueRow = result.Single(x =>
                x.Category == "Gesamtumsatz" &&
                x.Year == currentYear &&
                x.Quarter == currentQuarter);

            Assert.AreEqual(25.00m, revenueRow.Value);
        }
    }
}
