using OrderManagement.Application.Features.Catalog.Contracts;
using OrderManagement.Application.Features.Catalog.SearchArticles;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class SearchArticlesUseCaseTests
    {
        private static readonly string[] ExpectedDescendantArticleNumbers = ["ART-ROOT", "ART-CHILD", "ART-GRANDCHILD"];

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

        [TestMethod]
        public async Task ExecuteAsync_WithGroupIdHavingChildren_ShouldIncludeArticlesFromDescendantGroups()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new SearchArticlesUseCase(articleQueryRepository, groupQueryRepository);

            ArticleGroup root = groupQueryRepository.Seed(ArticleGroup.Create("Büro").EnsureValue());
            ArticleGroup child = groupQueryRepository.Seed(ArticleGroup.Create("Schreibwaren", root.Id).EnsureValue());
            ArticleGroup grandchild = groupQueryRepository.Seed(ArticleGroup.Create("Kugelschreiber", child.Id).EnsureValue());
            ArticleGroup unrelated = groupQueryRepository.Seed(ArticleGroup.Create("Werkzeuge").EnsureValue());

            _ = articleQueryRepository.Seed(Article.Create("ART-ROOT", "Root Article", 1m, "CHF", root.Id).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-CHILD", "Child Article", 1m, "CHF", child.Id).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-GRANDCHILD", "Grandchild Article", 1m, "CHF", grandchild.Id).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-UNRELATED", "Unrelated Article", 1m, "CHF", unrelated.Id).EnsureValue());

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new SearchArticlesQuery(null, root.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            string[] numbers = [.. result.Value!.Select(a => a.ArticleNumber)];
            CollectionAssert.AreEquivalent(ExpectedDescendantArticleNumbers, numbers);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithLeafGroupId_ShouldReturnOnlyThatGroupsArticles()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new SearchArticlesUseCase(articleQueryRepository, groupQueryRepository);

            ArticleGroup root = groupQueryRepository.Seed(ArticleGroup.Create("Büro").EnsureValue());
            ArticleGroup child = groupQueryRepository.Seed(ArticleGroup.Create("Schreibwaren", root.Id).EnsureValue());

            _ = articleQueryRepository.Seed(Article.Create("ART-ROOT", "Root Article", 1m, "CHF", root.Id).EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-CHILD", "Child Article", 1m, "CHF", child.Id).EnsureValue());

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new SearchArticlesQuery(null, child.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("ART-CHILD", result.Value[0].ArticleNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithActiveStatusFilter_ShouldExcludeInactiveArticles()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new SearchArticlesUseCase(articleQueryRepository, groupQueryRepository);

            _ = articleQueryRepository.Seed(Article.Create("ART-001", "Active Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());
            Article inactive = articleQueryRepository.Seed(
                Article.Create("ART-002", "Inactive Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());
            inactive.Deactivate().EnsureSuccess();

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(
                new SearchArticlesQuery(null, null, ArticleStatus.Active));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("ART-001", result.Value[0].ArticleNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithoutStatusFilter_ShouldIncludeInactiveArticles()
        {
            var articleQueryRepository = new FakeArticleQueryRepository();
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new SearchArticlesUseCase(articleQueryRepository, groupQueryRepository);

            _ = articleQueryRepository.Seed(Article.Create("ART-001", "Active Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());
            Article inactive = articleQueryRepository.Seed(
                Article.Create("ART-002", "Inactive Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());
            inactive.Deactivate().EnsureSuccess();

            Result<IReadOnlyList<ArticleListItemDto>> result = await useCase.ExecuteAsync(new SearchArticlesQuery(null, null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(2, result.Value!.Count);
        }
    }
}
