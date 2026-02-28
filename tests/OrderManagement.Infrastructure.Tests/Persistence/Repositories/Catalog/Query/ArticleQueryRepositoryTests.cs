using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog.Query
{
    [TestClass]
    public sealed class ArticleQueryRepositoryTests : IntegrationTestBase
    {
        private ArticleQueryRepository? _repository;
        private ArticleGroup? _testGroup;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Assert.IsNotNull(DbContext);
            _repository = new ArticleQueryRepository(DbContext);

            // Create test article group
            Result<ArticleGroup> groupResult = ArticleGroup.Create(200, "Test Group");
            Assert.IsTrue(groupResult.IsSuccess);
            _testGroup = groupResult.Value!;

            _ = DbContext.ArticleGroups.Add(_testGroup);
            _ = await DbContext.SaveChangesAsync();
        }

        [TestMethod]
        public async Task GetByIdAsync_ExistingArticle_ReturnsArticle()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testGroup);

            Result<Article> articleResult = Article.Create(
                id: 201,
                articleNr: "ART-001",
                name: "Test Article",
                priceAmount: 99.99m,
                priceCurrency: "EUR",
                groupId: _testGroup.Id.Value,
                stock: 10
            );

            Assert.IsTrue(articleResult.IsSuccess);
            _ = DbContext.Articles.Add(articleResult.Value!);
            _ = await DbContext.SaveChangesAsync();

            Article? retrieved = await _repository.GetByIdAsync(new ArticleId(201));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(201, retrieved.Id.Value);
            Assert.AreEqual("Test Article", retrieved.Name);
            Assert.AreEqual("ART-001", retrieved.ArticleNumber.Value);
        }

        [TestMethod]
        public async Task GetByIdAsync_NonExistingArticle_ReturnsNull()
        {
            Assert.IsNotNull(_repository);

            Article? result = await _repository.GetByIdAsync(new ArticleId(999));

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetListAsync_ReturnsAllArticles()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testGroup);

            for (int i = 202; i <= 206; i++)
            {
                Result<Article> result = Article.Create(
                    id: i,
                    articleNr: $"ART-{i:000}",
                    name: $"Article {i}",
                    priceAmount: i * 10m,
                    priceCurrency: "EUR",
                    groupId: _testGroup.Id.Value
                );

                Assert.IsTrue(result.IsSuccess);
                _ = DbContext.Articles.Add(result.Value!);
            }

            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<Article> articles = await _repository.GetListAsync();

            Assert.AreEqual(5, articles.Count);
        }

        [TestMethod]
        public async Task GetByNumberAsync_ExistingNumber_ReturnsArticle()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testGroup);

            Result<Article> articleResult = Article.Create(
                id: 207,
                articleNr: "UNIQUE-123",
                name: "Unique Article",
                priceAmount: 50.00m,
                priceCurrency: "EUR",
                groupId: _testGroup.Id.Value
            );

            Assert.IsTrue(articleResult.IsSuccess);
            _ = DbContext.Articles.Add(articleResult.Value!);
            _ = await DbContext.SaveChangesAsync();

            Result<ArticleNumber> numberResult = ArticleNumber.Create("UNIQUE-123");
            Assert.IsTrue(numberResult.IsSuccess);

            Article? retrieved = await _repository.GetByNumberAsync(numberResult.Value!);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Unique Article", retrieved.Name);
            Assert.AreEqual("UNIQUE-123", retrieved.ArticleNumber.Value);
        }

        [TestMethod]
        public async Task GetByNumberAsync_NonExistingNumber_ReturnsNull()
        {
            Assert.IsNotNull(_repository);

            Result<ArticleNumber> numberResult = ArticleNumber.Create("NOTFOUND");
            Assert.IsTrue(numberResult.IsSuccess);

            Article? result = await _repository.GetByNumberAsync(numberResult.Value!);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetByGroupAsync_ReturnsArticlesInGroup()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testGroup);

            // Create second group
            Result<ArticleGroup> group2Result = ArticleGroup.Create(201, "Group 2");
            Assert.IsTrue(group2Result.IsSuccess);
            _ = DbContext.ArticleGroups.Add(group2Result.Value!);

            // Add articles to first group
            for (int i = 208; i <= 210; i++)
            {
                Result<Article> result = Article.Create(
                    id: i,
                    articleNr: $"GRP1-{i:000}",
                    name: $"Group 1 Article {i}",
                    priceAmount: 10m,
                    priceCurrency: "EUR",
                    groupId: _testGroup.Id.Value
                );
                Assert.IsTrue(result.IsSuccess);
                _ = DbContext.Articles.Add(result.Value!);
            }

            // Add articles to second group
            for (int i = 211; i <= 212; i++)
            {
                Result<Article> result = Article.Create(
                    id: i,
                    articleNr: $"GRP2-{i:000}",
                    name: $"Group 2 Article {i}",
                    priceAmount: 10m,
                    priceCurrency: "EUR",
                    groupId: 201
                );
                Assert.IsTrue(result.IsSuccess);
                _ = DbContext.Articles.Add(result.Value!);
            }

            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<Article> group1Articles = await _repository.GetByGroupAsync(_testGroup.Id);

            Assert.AreEqual(3, group1Articles.Count);
            Assert.IsTrue(group1Articles.All(a => a.ArticleGroupId == _testGroup.Id));
        }

        [TestMethod]
        public async Task GetLowStockAsync_ReturnsArticlesBelowThreshold()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testGroup);

            // Create articles with different stock levels
            int[] stockLevels = [2, 5, 10, 15, 20];
            for (int i = 0; i < stockLevels.Length; i++)
            {
                Result<Article> result = Article.Create(
                    id: i + 1,
                    articleNr: $"STK-{i + 1:000}",
                    name: $"Article {i + 1}",
                    priceAmount: 10m,
                    priceCurrency: "EUR",
                    groupId: _testGroup.Id.Value,
                    stock: stockLevels[i],
                    status: 1
                );
                Assert.IsTrue(result.IsSuccess);
                _ = DbContext.Articles.Add(result.Value!);
            }

            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<Article> lowStockArticles = await _repository.GetLowStockAsync(threshold: 10);

            // Should return articles with stock < 10 (stock: 2 and 5)
            Assert.AreEqual(2, lowStockArticles.Count);
            Assert.IsTrue(lowStockArticles.All(a => a.Stock < 10));
        }

        [TestMethod]
        public async Task GetLowStockAsync_ExcludesInactiveArticles()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testGroup);

            // Active article with low stock
            Result<Article> activeResult = Article.Create(
                id: 1,
                articleNr: "ACTIVE-001",
                name: "Active Low Stock",
                priceAmount: 10m,
                priceCurrency: "EUR",
                groupId: _testGroup.Id.Value,
                stock: 3,
                status: 1
            );
            Assert.IsTrue(activeResult.IsSuccess);
            _ = DbContext.Articles.Add(activeResult.Value!);

            // Inactive article with low stock
            Result<Article> inactiveResult = Article.Create(
                id: 2,
                articleNr: "INACTIVE-001",
                name: "Inactive Low Stock",
                priceAmount: 10m,
                priceCurrency: "EUR",
                groupId: _testGroup.Id.Value,
                stock: 2,
                status: 0
            );
            Assert.IsTrue(inactiveResult.IsSuccess);
            _ = DbContext.Articles.Add(inactiveResult.Value!);

            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<Article> lowStockArticles = await _repository.GetLowStockAsync(threshold: 10);

            // Should only return active article
            Assert.AreEqual(1, lowStockArticles.Count);
            Assert.AreEqual(1, lowStockArticles[0].Status);
            Assert.AreEqual("ACTIVE-001", lowStockArticles[0].ArticleNumber.Value);
        }
    }
}
