using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Catalog.Query
{
    [TestClass]
    public sealed class ArticleQueryRepositoryTests : IntegrationTestBase
    {
        private ArticleQueryRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new ArticleQueryRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task GetByIdAsync_WithExistingArticle_ShouldReturnArticle()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext);
            DbContext.ChangeTracker.Clear();

            Article? result = await _repository.GetByIdAsync(article.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(article.Id, result.Id);
        }

        [TestMethod]
        public async Task GetByNumberAsync_WithExistingArticleNumber_ShouldReturnArticle()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(
                DbContext,
                articleNumber: "ART-220001");

            ArticleNumber number = ArticleNumber.Create("ART-220001").EnsureValue();
            DbContext.ChangeTracker.Clear();

            Article? result = await _repository.GetByNumberAsync(number);

            Assert.IsNotNull(result);
            Assert.AreEqual(article.Id, result.Id);
        }

        [TestMethod]
        public async Task GetByGroupAsync_WithExistingGroup_ShouldReturnOnlyArticlesFromGroup()
        {
            ArticleGroup group1 = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Group 1");
            ArticleGroup group2 = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Group 2");

            Article article1 = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, group1.Id);
            Article article2 = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, group1.Id);
            _ = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, group2.Id);

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Article> result = await _repository.GetByGroupAsync(group1.Id);

            CollectionAssert.AreEquivalent(
                new[] { article1.Id, article2.Id },
                result.Select(a => a.Id).ToArray());
        }

        [TestMethod]
        public async Task GetLowStockAsync_ShouldReturnOnlyActiveArticlesAtOrBelowTheirOwnReorderPoint()
        {
            Article lowActive = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(
                DbContext,
                stock: 2,
                reorderPoint: 5,
                status: ArticleStatus.Active);

            _ = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(
                DbContext,
                stock: 20,
                reorderPoint: 5,
                status: ArticleStatus.Active);

            _ = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(
                DbContext,
                stock: 1,
                reorderPoint: 5,
                status: ArticleStatus.Inactive);

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Article> result = await _repository.GetLowStockAsync();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(lowActive.Id, result.Single().Id);
        }

        [TestMethod]
        public async Task GetLowStockAsync_ShouldIncludeArticleWhenStockEqualsReorderPoint()
        {
            Article boundaryArticle = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(
                DbContext,
                stock: 5,
                reorderPoint: 5,
                status: ArticleStatus.Active);

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Article> result = await _repository.GetLowStockAsync();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(boundaryArticle.Id, result.Single().Id);
            Assert.AreEqual(StockLevel.Low, result.Single().StockLevel);
        }

        [TestMethod]
        public async Task GetLowStockAsync_WithTwoArticlesHavingTheSameStockButDifferentReorderPoints_ShouldOnlyReturnTheOneBelowItsOwnReorderPoint()
        {
            Article strict = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(
                DbContext,
                stock: 10,
                reorderPoint: 15,
                status: ArticleStatus.Active);

            _ = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(
                DbContext,
                stock: 10,
                reorderPoint: 5,
                status: ArticleStatus.Active);

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Article> result = await _repository.GetLowStockAsync();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(strict.Id, result.Single().Id);
        }

        [TestMethod]
        public async Task GetLowStockAsync_ShouldDistinguishLowFromOutOfStock()
        {
            Article low = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(
                DbContext,
                stock: 2,
                reorderPoint: 5,
                status: ArticleStatus.Active);

            Article depleted = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(
                DbContext,
                stock: 0,
                reorderPoint: 5,
                status: ArticleStatus.Active);

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Article> result = await _repository.GetLowStockAsync();

            Assert.AreEqual(StockLevel.Low, result.Single(a => a.Id == low.Id).StockLevel);
            Assert.AreEqual(StockLevel.OutOfStock, result.Single(a => a.Id == depleted.Id).StockLevel);
        }
    }
}
