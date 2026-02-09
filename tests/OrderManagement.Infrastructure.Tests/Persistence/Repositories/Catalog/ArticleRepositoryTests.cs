using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog
{
    [TestClass]
    public class ArticleRepositoryTests : RepositoryTestBase
    {
        private readonly ArticleRepository _sut;
        private readonly Article? _article1;
        private readonly Article? _article2;

        public ArticleRepositoryTests()
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

            SharedKernel.Primitives.Result<Article> createResult2 = Article.Create(
                id: 2,
                articleNr: "A002",
                name: "Widget B",
                priceAmount: 5.99m,
                priceCurrency: "CHF",
                groupId: 100,
                stock: 5);

            Assert.IsTrue(createResult1.IsSuccess);
            Assert.IsTrue(createResult2.IsSuccess);

            _article1 = createResult1.Value;
            _article2 = createResult2.Value;

            Context.Articles.AddRange(_article1!, _article2!);
            _ = Context.SaveChanges();

            _sut = new ArticleRepository(Context);
        }

        [TestMethod]
        public async Task GetByIdAsyncReturnsCorrectArticle()
        {
            Article? result = await _sut.GetByIdAsync(_article1!.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual("Widget A", result.Name);
            Assert.AreEqual("A001", result.ArticleNumber.Value);
            Assert.AreEqual(100, result.Stock);
        }

        [TestMethod]
        public async Task GetByIdAsyncNotFoundReturnsNull()
        {
            Article? result = await _sut.GetByIdAsync(new ArticleId(999));

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetListAsyncReturnsAllArticles()
        {
            IReadOnlyList<Article> result = await _sut.GetListAsync();

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task GetByNumberAsyncFindsByNumber()
        {
            Article? result = await _sut.GetByNumberAsync(_article1!.ArticleNumber);

            Assert.IsNotNull(result);
            Assert.AreEqual("Widget A", result!.Name);
        }

        [TestMethod]
        public async Task GetByNumberAsyncNotFoundReturnsNull()
        {
            SharedKernel.Primitives.Result<ArticleNumber> numberResult = ArticleNumber.Create("NONEXISTENT");
            Assert.IsTrue(numberResult.IsSuccess);
            ArticleNumber number = numberResult.Value!;
            Article? result = await _sut.GetByNumberAsync(number);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetByGroupAsyncReturnsGroupArticles()
        {
            IReadOnlyList<Article> result = await _sut.GetByGroupAsync(_article1!.ArticleGroupId);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task GetByGroupAsyncEmptyGroupReturnsEmpty()
        {
            IReadOnlyList<Article> result = await _sut.GetByGroupAsync(new ArticleGroupId(999));

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task GetLowStockAsyncFiltersLowStockActive()
        {
            IReadOnlyList<Article> result = await _sut.GetLowStockAsync(10);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Widget B", result[0].Name);
            Assert.AreEqual(5, result[0].Stock);
        }

        [TestMethod]
        public async Task GetLowStockAsyncNoLowStockReturnsEmpty()
        {
            IReadOnlyList<Article> result = await _sut.GetLowStockAsync(1);

            Assert.AreEqual(0, result.Count);
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

            Assert.AreEqual(3, Context.Articles.Count());


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

            Assert.AreEqual(1, Context.Articles.Count());
        }
    }
}
