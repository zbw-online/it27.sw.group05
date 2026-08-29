using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.GetArticleForEdit;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class GetArticleForEditUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingArticle_ShouldReturnDetails()
        {
            var queryRepository = new FakeArticleQueryRepository();
            var useCase = new GetArticleForEditUseCase(queryRepository);

            Article article = queryRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Result<GetArticleForEditResponse> result = await useCase.ExecuteAsync(new GetArticleForEditQuery(article.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("ART-001", result.Value!.ArticleNumber);
            Assert.AreEqual(5, result.Value.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownArticle_ShouldFail()
        {
            var queryRepository = new FakeArticleQueryRepository();
            var useCase = new GetArticleForEditUseCase(queryRepository);

            Result<GetArticleForEditResponse> result = await useCase.ExecuteAsync(new GetArticleForEditQuery(999));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
