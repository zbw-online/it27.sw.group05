using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Catalog.Command
{
    [TestClass]
    public sealed class ArticleCommandRepositoryTests : IntegrationTestBase
    {
        private ArticleCommandRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new ArticleCommandRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task Add_WithExistingGroup_ShouldPersistArticleAndGenerateId()
        {
            ArticleGroup group = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext);

            Article article = Article.Create(
                articleNr: "ART-210001",
                name: "Test Article",
                priceAmount: 42.50m,
                priceCurrency: "CHF",
                groupId: group.Id,
                stock: 5,
                vatRate: 7.70m).EnsureValue();

            _repository.Add(article);
            _ = await DbContext.SaveChangesAsync();

            Assert.IsTrue(article.Id.IsAssigned);

            DbContext.ChangeTracker.Clear();

            Article? persisted = await DbContext.Articles
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == article.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual("ART-210001", persisted.ArticleNumber.Value);
            Assert.AreEqual(group.Id, persisted.ArticleGroupId);
            Assert.AreEqual(42.50m, persisted.Price.Amount);
            Assert.AreEqual("CHF", persisted.Price.Currency);
        }

        [TestMethod]
        public async Task Update_WithChangedPrice_ShouldPersistPrice()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, priceAmount: 10.00m);
            ArticleId articleId = article.Id;

            DbContext.ChangeTracker.Clear();

            Article tracked = await DbContext.Articles.SingleAsync(a => a.Id == articleId);
            Result changeResult = tracked.ChangePrice(Money.From(99.95m, "CHF").EnsureValue());
            Assert.IsTrue(changeResult.IsSuccess, changeResult.Error);

            _repository.Update(tracked);
            _ = await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();

            Article? persisted = await DbContext.Articles
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == articleId);

            Assert.IsNotNull(persisted);
            Assert.AreEqual(99.95m, persisted.Price.Amount);
        }

        [TestMethod]
        public async Task Remove_WithExistingArticle_ShouldDeleteArticle()
        {
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext);
            ArticleId articleId = article.Id;

            DbContext.ChangeTracker.Clear();

            Article tracked = await DbContext.Articles.SingleAsync(a => a.Id == articleId);

            _repository.Remove(tracked);
            _ = await DbContext.SaveChangesAsync();

            bool exists = await DbContext.Articles.AsNoTracking().AnyAsync(a => a.Id == articleId);

            Assert.IsFalse(exists);
        }
    }
}
