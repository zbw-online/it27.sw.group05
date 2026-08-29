using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.DTOs.Catalog;
using OrderManagement.Application.Features.Catalog.GetArticleGroupHierarchy;
using OrderManagement.Application.Tests.Fakes.Catalog;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class GetArticleGroupHierarchyUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithoutRootGroupId_ShouldRequestFullHierarchy()
        {
            var groupQueryRepository = new FakeArticleGroupQueryRepository
            {
                HierarchyResult = [new ArticleGroupHierarchyDto(1, "Electronics", null, 0, "Electronics")]
            };
            var useCase = new GetArticleGroupHierarchyUseCase(groupQueryRepository);

            Result<IReadOnlyList<ArticleGroupHierarchyDto>> result = await useCase.ExecuteAsync(new GetArticleGroupHierarchyQuery(null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(groupQueryRepository.FullHierarchyCalled);
            Assert.AreEqual(1, result.Value!.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithRootGroupId_ShouldRequestHierarchyFromThatRoot()
        {
            var groupQueryRepository = new FakeArticleGroupQueryRepository
            {
                HierarchyResult = [new ArticleGroupHierarchyDto(2, "Phones", 1, 1, "Electronics/Phones")]
            };
            var useCase = new GetArticleGroupHierarchyUseCase(groupQueryRepository);

            Result<IReadOnlyList<ArticleGroupHierarchyDto>> result = await useCase.ExecuteAsync(new GetArticleGroupHierarchyQuery(1));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, groupQueryRepository.HierarchyFromRootCalledWith!.Value.Value);
            Assert.AreEqual(1, result.Value!.Count);
        }
    }
}
