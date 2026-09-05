using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.DeactivateArticle;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class DeactivateArticleUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithActiveArticle_ShouldDeactivateAndCommit()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeactivateArticleUseCase(commandRepository, unitOfWork);

            Article article = commandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());

            Result result = await useCase.ExecuteAsync(new DeactivateArticleCommand(article.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(ArticleStatus.Inactive, article.Status);
            Assert.AreEqual(1, commandRepository.Updated.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithAlreadyInactiveArticle_ShouldFailAndNotCommit()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeactivateArticleUseCase(commandRepository, unitOfWork);

            Article article = Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue();
            article.Deactivate().EnsureSuccess();
            _ = commandRepository.Seed(article);

            Result result = await useCase.ExecuteAsync(new DeactivateArticleCommand(article.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownArticle_ShouldFail()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeactivateArticleUseCase(commandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new DeactivateArticleCommand(999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
