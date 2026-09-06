using OrderManagement.Application.Features.Catalog.CreateArticleGroup;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class CreateArticleGroupUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithoutParent_ShouldCreateRootGroupAndCommit()
        {
            var commandRepository = new FakeArticleGroupCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateArticleGroupUseCase(commandRepository, unitOfWork);

            Result<CreateArticleGroupResponse> result = await useCase.ExecuteAsync(new CreateArticleGroupCommand("Electronics", null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Electronics", result.Value!.Name);
            Assert.IsNull(result.Value.ParentGroupId);
            Assert.AreEqual(1, commandRepository.Added.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithExistingParent_ShouldCreateChildGroup()
        {
            var commandRepository = new FakeArticleGroupCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateArticleGroupUseCase(commandRepository, unitOfWork);

            ArticleGroup parent = commandRepository.Seed(ArticleGroup.Create("Electronics").EnsureValue());

            Result<CreateArticleGroupResponse> result = await useCase.ExecuteAsync(
                new CreateArticleGroupCommand("Phones", parent.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(parent.Id.Value, result.Value!.ParentGroupId);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownParent_ShouldFail()
        {
            var commandRepository = new FakeArticleGroupCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateArticleGroupUseCase(commandRepository, unitOfWork);

            Result<CreateArticleGroupResponse> result = await useCase.ExecuteAsync(
                new CreateArticleGroupCommand("Phones", 999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, commandRepository.Added.Count);
        }
    }
}
