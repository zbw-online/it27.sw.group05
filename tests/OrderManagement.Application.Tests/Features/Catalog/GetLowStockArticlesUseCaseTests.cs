using OrderManagement.Application.Features.Catalog.Contracts;
using OrderManagement.Application.Features.Catalog.GetLowStockArticles;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class GetLowStockArticlesUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_ShouldReturnOnlyArticlesAtOrBelowTheirOwnReorderPoint()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new GetLowStockArticlesUseCase(articleQueryRepository, groupQueryRepository);

            _ = articleQueryRepository.Seed(Article.Create("ART-001", "Low", 9.99m, "CHF", new ArticleGroupId(1), stock: 2, reorderPoint: 5).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-002", "High", 5.00m, "CHF", new ArticleGroupId(1), stock: 50, reorderPoint: 5).EnsureValue());

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new GetLowStockArticlesQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("ART-001", result.Value[0].ArticleNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithTwoArticlesHavingTheSameStockButDifferentReorderPoints_ShouldOnlyReturnTheOneBelowItsOwnReorderPoint()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new GetLowStockArticlesUseCase(articleQueryRepository, groupQueryRepository);

            _ = articleQueryRepository.Seed(Article.Create("ART-001", "Strict", 9.99m, "CHF", new ArticleGroupId(1), stock: 10, reorderPoint: 15).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-002", "Lenient", 9.99m, "CHF", new ArticleGroupId(1), stock: 10, reorderPoint: 5).EnsureValue());

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new GetLowStockArticlesQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("ART-001", result.Value[0].ArticleNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldDistinguishLowFromOutOfStockInTheReturnedDto()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new GetLowStockArticlesUseCase(articleQueryRepository, groupQueryRepository);

            _ = articleQueryRepository.Seed(Article.Create("ART-001", "Low", 9.99m, "CHF", new ArticleGroupId(1), stock: 2, reorderPoint: 5).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-002", "Depleted", 9.99m, "CHF", new ArticleGroupId(1), stock: 0, reorderPoint: 5).EnsureValue());

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new GetLowStockArticlesQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(StockLevel.Low, result.Value!.Single(a => a.ArticleNumber == "ART-001").StockLevel);
            Assert.AreEqual(StockLevel.OutOfStock, result.Value!.Single(a => a.ArticleNumber == "ART-002").StockLevel);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldExcludeInactiveArticlesEvenWhenBelowReorderPoint()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new GetLowStockArticlesUseCase(articleQueryRepository, groupQueryRepository);

            Article inactive = Article.Create("ART-001", "Inactive", 9.99m, "CHF", new ArticleGroupId(1), stock: 1, reorderPoint: 20).EnsureValue();
            _ = inactive.Deactivate();
            _ = articleQueryRepository.Seed(inactive);

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new GetLowStockArticlesQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(0, result.Value!.Count);
        }
    }
}
