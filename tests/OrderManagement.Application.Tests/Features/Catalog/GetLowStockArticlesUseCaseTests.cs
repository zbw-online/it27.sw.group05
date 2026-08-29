using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.GetLowStockArticles;
using OrderManagement.Application.Features.Catalog.Shared;
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
        public async Task ExecuteAsync_WithThreshold_ShouldReturnOnlyArticlesAtOrBelowThreshold()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new GetLowStockArticlesUseCase(articleQueryRepository, groupQueryRepository);

            _ = articleQueryRepository.Seed(Article.Create("ART-001", "Low", 9.99m, "CHF", new ArticleGroupId(1), stock: 2).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-002", "High", 5.00m, "CHF", new ArticleGroupId(1), stock: 50).EnsureValue());

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new GetLowStockArticlesQuery(5));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("ART-001", result.Value[0].ArticleNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithNegativeThreshold_ShouldFail()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new GetLowStockArticlesUseCase(articleQueryRepository, groupQueryRepository);

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new GetLowStockArticlesQuery(-1));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
