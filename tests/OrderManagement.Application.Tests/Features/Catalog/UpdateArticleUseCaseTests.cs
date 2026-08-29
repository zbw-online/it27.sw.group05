using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.UpdateArticle;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class UpdateArticleUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithChangedPriceGroupAndStock_ShouldApplyAllChangesAndCommit()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateArticleUseCase(commandRepository, unitOfWork);

            Article article = commandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Result result = await useCase.ExecuteAsync(new UpdateArticleCommand(
                article.Id.Value, "Widget", 12.50m, "CHF", 2, 8, 7.7m, "Updated"));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(12.50m, article.Price.Amount);
            Assert.AreEqual(2, article.ArticleGroupId.Value);
            Assert.AreEqual(8, article.Stock);
            Assert.AreEqual(1, commandRepository.Updated.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownArticle_ShouldFail()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateArticleUseCase(commandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new UpdateArticleCommand(
                999, "Widget", 12.50m, "CHF", 2, 8, 7.7m, null));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithStockReductionBelowZero_ShouldFail()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateArticleUseCase(commandRepository, unitOfWork);

            Article article = commandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Result result = await useCase.ExecuteAsync(new UpdateArticleCommand(
                article.Id.Value, "Widget", 9.99m, "CHF", 1, -10, 7.7m, null));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, commandRepository.Updated.Count);
        }
    }
}
