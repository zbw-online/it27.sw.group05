using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.DeleteArticle;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class DeleteArticleUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingArticle_ShouldRemoveAndCommit()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteArticleUseCase(commandRepository, unitOfWork);

            Article article = commandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());

            Result result = await useCase.ExecuteAsync(new DeleteArticleCommand(article.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, commandRepository.Removed.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownArticle_ShouldFail()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteArticleUseCase(commandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new DeleteArticleCommand(999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
