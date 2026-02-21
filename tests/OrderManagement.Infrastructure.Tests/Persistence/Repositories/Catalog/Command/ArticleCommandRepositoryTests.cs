using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog.Command
{
    [TestClass]
    public sealed class ArticleCommandRepositoryTests : IntegrationTestBase
    {
        private ArticleCommandRepository? _repository;
        private ArticleGroup? _testArticleGroup;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Assert.IsNotNull(DbContext);
            _repository = new ArticleCommandRepository(DbContext);

            Result<ArticleGroup> groupResult = ArticleGroup.Create(100, "Test Article Group", parentGroupId: null);
            Assert.IsTrue(groupResult.IsSuccess);
            _testArticleGroup = groupResult.Value!;

            _ = DbContext.ArticleGroups.Add(_testArticleGroup);
            _ = await DbContext.SaveChangesAsync();
        }

        [TestMethod]
        public async Task Add_ShouldPersistArticleToDatabase()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testArticleGroup);

            Result<Article> articleResult = Article.Create(
                id: 101,
                articleNr: "ART-001",
                name: "Test Article",
                priceAmount: 99.99m,
                priceCurrency: "EUR",
                groupId: _testArticleGroup.Id.Value,
                stock: 10,
                vatRate: 19.00m,
                description: "Integration test article"
            );

            Assert.IsTrue(articleResult.IsSuccess);
            Article article = articleResult.Value!;

            _repository.Add(article);
            _ = await DbContext.SaveChangesAsync();

            Article? retrieved = await DbContext.Articles
                .FirstOrDefaultAsync(a => a.Id == new ArticleId(101));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("ART-001", retrieved.ArticleNumber.Value);
            Assert.AreEqual("Test Article", retrieved.Name);
            Assert.AreEqual(99.99m, retrieved.Price.Amount);
            Assert.AreEqual("EUR", retrieved.Price.Currency);
            Assert.AreEqual(10, retrieved.Stock);
        }

        [TestMethod]
        public async Task Update_ShouldModifyExistingArticle()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testArticleGroup);

            Result<Article> articleResult = Article.Create(
                id: 102,
                articleNr: "ART-002",
                name: "Original Name",
                priceAmount: 50.00m,
                priceCurrency: "EUR",
                groupId: _testArticleGroup.Id.Value
            );

            Assert.IsTrue(articleResult.IsSuccess);
            Article article = articleResult.Value!;

            _ = DbContext.Articles.Add(article);
            _ = await DbContext.SaveChangesAsync();
            DbContext.Entry(article).State = EntityState.Detached;

            Article? tracked = await DbContext.Articles
                .FirstOrDefaultAsync(a => a.Id == new ArticleId(102));

            Assert.IsNotNull(tracked);

            Money newPrice = Money.From(75.00m, "EUR").EnsureValue();
            Result priceChangeResult = tracked.ChangePrice(newPrice);
            Assert.IsTrue(priceChangeResult.IsSuccess);

            _repository.Update(tracked);
            _ = await DbContext.SaveChangesAsync();

            Article? updated = await DbContext.Articles
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == new ArticleId(102));

            Assert.IsNotNull(updated);
            Assert.AreEqual(75.00m, updated.Price.Amount);
        }

        [TestMethod]
        public async Task Remove_ShouldDeleteArticleFromDatabase()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testArticleGroup);

            Result<Article> articleResult = Article.Create(
                id: 103,
                articleNr: "ART-003",
                name: "To Be Deleted",
                priceAmount: 10.00m,
                priceCurrency: "EUR",
                groupId: _testArticleGroup.Id.Value
            );

            Assert.IsTrue(articleResult.IsSuccess);
            Article article = articleResult.Value!;

            _ = DbContext.Articles.Add(article);
            _ = await DbContext.SaveChangesAsync();

            _repository.Remove(article);
            _ = await DbContext.SaveChangesAsync();

            Article? deleted = await DbContext.Articles
                .FirstOrDefaultAsync(a => a.Id == new ArticleId(103));

            Assert.IsNull(deleted);
        }

        [TestMethod]
        public async Task Add_MultipleArticles_ShouldPersistAll()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_testArticleGroup);

            for (int i = 10; i < 15; i++)
            {
                Result<Article> result = Article.Create(
                    id: i,
                    articleNr: $"ART-{i:000}",
                    name: $"Article {i}",
                    priceAmount: i * 10m,
                    priceCurrency: "EUR",
                    groupId: _testArticleGroup.Id.Value
                );

                Assert.IsTrue(result.IsSuccess);
                _repository.Add(result.Value!);
            }

            _ = await DbContext.SaveChangesAsync();

            int count = await DbContext.Articles.CountAsync();
            Assert.AreEqual(5, count);
        }
    }
}
