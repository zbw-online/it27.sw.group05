using OrderManagement.Application.Features.Catalog.DeleteArticleGroup;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class DeleteArticleGroupUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithEmptyLeafGroup_ShouldRemoveAndCommit()
        {
            var groupCommandRepository = new FakeArticleGroupCommandRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteArticleGroupUseCase(groupCommandRepository, articleQueryRepository, unitOfWork);

            ArticleGroup group = groupCommandRepository.Seed(ArticleGroup.Create("Empty Group").EnsureValue());

            Result result = await useCase.ExecuteAsync(new DeleteArticleGroupCommand(group.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, groupCommandRepository.Removed.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithGroupContainingArticles_ShouldFail()
        {
            var groupCommandRepository = new FakeArticleGroupCommandRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteArticleGroupUseCase(groupCommandRepository, articleQueryRepository, unitOfWork);

            ArticleGroup group = groupCommandRepository.Seed(ArticleGroup.Create("Occupied Group").EnsureValue());
            _ = articleQueryRepository.Seed(Article.Create("ART-001", "Widget", 9.99m, "CHF", group.Id).EnsureValue());

            Result result = await useCase.ExecuteAsync(new DeleteArticleGroupCommand(group.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "articles");
            Assert.AreEqual(0, groupCommandRepository.Removed.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithGroupContainingChildGroups_ShouldFail()
        {
            var groupCommandRepository = new FakeArticleGroupCommandRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteArticleGroupUseCase(groupCommandRepository, articleQueryRepository, unitOfWork);

            ArticleGroup parent = groupCommandRepository.Seed(ArticleGroup.Create("Parent").EnsureValue());
            ArticleGroup child = ArticleGroup.Create("Child", parent.Id).EnsureValue();
            parent.AddChild(child).EnsureSuccess();

            Result result = await useCase.ExecuteAsync(new DeleteArticleGroupCommand(parent.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "child groups");
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownGroup_ShouldFail()
        {
            var groupCommandRepository = new FakeArticleGroupCommandRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteArticleGroupUseCase(groupCommandRepository, articleQueryRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new DeleteArticleGroupCommand(999));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
