using OrderManagement.Domain.Catalog;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog.Command
{
    [TestClass]
    public class ArticleCommandRepositoryTests : RepositoryTestBase
    {
        private readonly ArticleCommandRepository _sut;
        private readonly Article? _article1;

        public ArticleCommandRepositoryTests()
        {
            SharedKernel.Primitives.Result<ArticleGroup> groupResult = ArticleGroup.Create(
                id: 100,
                name: "Test Group",
                parentGroupId: null);

            Assert.IsTrue(groupResult.IsSuccess);
            _ = Context.ArticleGroups.Add(groupResult.Value!);
            _ = Context.SaveChanges();

            SharedKernel.Primitives.Result<Article> createResult1 = Article.Create(
                id: 1,
                articleNr: "A001",
                name: "Widget A",
                priceAmount: 10.99m,
                priceCurrency: "CHF",
                groupId: 100,
                stock: 100);

            Assert.IsTrue(createResult1.IsSuccess);

            _article1 = createResult1.Value;

            _ = Context.Articles.Add(_article1!);
            _ = Context.SaveChanges();

            _sut = new ArticleCommandRepository(Context);
        }

        [TestMethod]
        public void AddSavesNewArticle()
        {
            SharedKernel.Primitives.Result<ArticleGroup> groupResult = ArticleGroup.Create(
                id: 200,
                name: "New Group",
                parentGroupId: null);
            Assert.IsTrue(groupResult.IsSuccess);
            _ = Context.ArticleGroups.Add(groupResult.Value!);
            _ = Context.SaveChanges();

            SharedKernel.Primitives.Result<Article> createResult = Article.Create(
                id: 3,
                articleNr: "A003",
                name: "New Widget",
                priceAmount: 15.99m,
                priceCurrency: "CHF",
                groupId: 200,
                stock: 50);

            Assert.IsTrue(createResult.IsSuccess);
            Article newArticle = createResult.Value!;

            _sut.Add(newArticle);
            _ = Context.SaveChanges();

            Assert.AreEqual(2, Context.Articles.Count());

            Article? saved = Context.Articles.ToList().FirstOrDefault(a => a.ArticleNumber.Value == "A003");
            Assert.IsNotNull(saved);
            Assert.AreEqual("New Widget", saved!.Name);
            Assert.AreEqual("A003", saved.ArticleNumber.Value);
        }

        [TestMethod]
        public void UpdateUpdatesExisting()
        {
            Article article = Context.Articles.First();
            SharedKernel.Primitives.Result result = article.UpdateStock(200 - article.Stock);
            Assert.IsTrue(result.IsSuccess);

            _sut.Update(article);
            _ = Context.SaveChanges();

            Article updated = Context.Articles.First();
            Assert.AreEqual(200, updated.Stock);
        }

        [TestMethod]
        public void RemoveRemovesArticle()
        {
            Article article = Context.Articles.First();
            _sut.Remove(article);
            _ = Context.SaveChanges();

            Assert.AreEqual(0, Context.Articles.Count());
        }
    }
}
