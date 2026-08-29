using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.SearchArticles;
using OrderManagement.Application.Features.Catalog.Shared;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class SearchArticlesUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithSearchTerm_ShouldReturnOnlyMatchingArticles()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new SearchArticlesUseCase(articleQueryRepository, groupQueryRepository);

            _ = articleQueryRepository.Seed(Article.Create("ART-001", "Blue Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-002", "Red Gadget", 5.00m, "CHF", new ArticleGroupId(1)).EnsureValue());

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new SearchArticlesQuery("Widget", null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("ART-001", result.Value[0].ArticleNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithGroupId_ShouldFilterArticlesByGroup()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new SearchArticlesUseCase(articleQueryRepository, groupQueryRepository);

            _ = articleQueryRepository.Seed(Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-002", "Gadget", 5.00m, "CHF", new ArticleGroupId(2)).EnsureValue());

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new SearchArticlesQuery(null, 2));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("ART-002", result.Value[0].ArticleNumber);
        }
    }
}
