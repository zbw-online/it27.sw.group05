using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.CreateArticle;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class CreateArticleUseCaseTests
    {
        private static CreateArticleCommand ValidCommand(string articleNumber = "ART-001")
            => new(articleNumber, "Widget", 9.99m, "CHF", 1, 10, 7.7m, "A useful widget");

        [TestMethod]
        public async Task ExecuteAsync_WithValidCommand_ShouldPersistArticleAndCommit()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var queryRepository = new FakeArticleQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateArticleUseCase(commandRepository, queryRepository, unitOfWork);

            Result<CreateArticleResponse> result = await useCase.ExecuteAsync(ValidCommand());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("ART-001", result.Value!.ArticleNumber);
            Assert.AreEqual(1, commandRepository.Added.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDuplicateArticleNumber_ShouldFailAndNotCommit()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var queryRepository = new FakeArticleQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateArticleUseCase(commandRepository, queryRepository, unitOfWork);

            _ = queryRepository.Seed(Article.Create("ART-001", "Existing", 1m, "CHF", new ArticleGroupId(1)).EnsureValue());

            Result<CreateArticleResponse> result = await useCase.ExecuteAsync(ValidCommand());

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "already exists");
            Assert.AreEqual(0, commandRepository.Added.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithNegativeStock_ShouldFailBeforeTouchingRepositories()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var queryRepository = new FakeArticleQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateArticleUseCase(commandRepository, queryRepository, unitOfWork);

            Result<CreateArticleResponse> result = await useCase.ExecuteAsync(
                new CreateArticleCommand("ART-002", "Widget", 9.99m, "CHF", 1, -1, 7.7m, null));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, commandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
