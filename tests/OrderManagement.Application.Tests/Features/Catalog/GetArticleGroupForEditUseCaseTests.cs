using OrderManagement.Application.Features.Catalog.GetArticleGroupForEdit;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class GetArticleGroupForEditUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingGroup_ShouldReturnDetails()
        {
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new GetArticleGroupForEditUseCase(groupQueryRepository);

            ArticleGroup group = groupQueryRepository.Seed(ArticleGroup.Create("Electronics").EnsureValue());

            Result<GetArticleGroupForEditResponse> result = await useCase.ExecuteAsync(new GetArticleGroupForEditQuery(group.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Electronics", result.Value!.Name);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownGroup_ShouldFail()
        {
            var groupQueryRepository = new FakeArticleGroupQueryRepository();
            var useCase = new GetArticleGroupForEditUseCase(groupQueryRepository);

            Result<GetArticleGroupForEditResponse> result = await useCase.ExecuteAsync(new GetArticleGroupForEditQuery(999));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
