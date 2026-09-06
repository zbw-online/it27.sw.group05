using OrderManagement.Application.Features.Catalog.Contracts;
using OrderManagement.Application.Features.Catalog.SearchArticleGroups;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class SearchArticleGroupsUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithSearchTerm_ShouldReturnOnlyMatchingGroups()
        {
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new SearchArticleGroupsUseCase(groupQueryRepository);

            _ = groupQueryRepository.Seed(ArticleGroup.Create("Electronics").EnsureValue());
            _ = groupQueryRepository.Seed(ArticleGroup.Create("Furniture").EnsureValue());

            Result<IReadOnlyList<ArticleGroupListItemDto>> result = await useCase.ExecuteAsync(new SearchArticleGroupsQuery("Elect", null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("Electronics", result.Value[0].Name);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithParentGroupId_ShouldReturnOnlyDirectChildren()
        {
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new SearchArticleGroupsUseCase(groupQueryRepository);

            ArticleGroup parent = groupQueryRepository.Seed(ArticleGroup.Create("Electronics").EnsureValue());
            _ = groupQueryRepository.Seed(ArticleGroup.Create("Phones", parent.Id).EnsureValue());
            _ = groupQueryRepository.Seed(ArticleGroup.Create("Furniture").EnsureValue());

            Result<IReadOnlyList<ArticleGroupListItemDto>> result = await useCase.ExecuteAsync(
                new SearchArticleGroupsQuery(null, parent.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("Phones", result.Value[0].Name);
        }
    }
}
