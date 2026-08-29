using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.RenameArticleGroup;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class RenameArticleGroupUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithValidName_ShouldRenameAndCommit()
        {
            var commandRepository = new FakeArticleGroupCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new RenameArticleGroupUseCase(commandRepository, unitOfWork);

            ArticleGroup group = commandRepository.Seed(ArticleGroup.Create("Old Name").EnsureValue());

            Result result = await useCase.ExecuteAsync(new RenameArticleGroupCommand(group.Id.Value, "New Name"));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("New Name", group.Name);
            Assert.AreEqual(1, commandRepository.Updated.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithEmptyName_ShouldFail()
        {
            var commandRepository = new FakeArticleGroupCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new RenameArticleGroupUseCase(commandRepository, unitOfWork);

            ArticleGroup group = commandRepository.Seed(ArticleGroup.Create("Old Name").EnsureValue());

            Result result = await useCase.ExecuteAsync(new RenameArticleGroupCommand(group.Id.Value, "   "));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Old Name", group.Name);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownGroup_ShouldFail()
        {
            var commandRepository = new FakeArticleGroupCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new RenameArticleGroupUseCase(commandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new RenameArticleGroupCommand(999, "New Name"));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
