using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.UpdateArticleStock;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class UpdateArticleStockUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithPositiveDelta_ShouldIncreaseStockAndCommit()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateArticleStockUseCase(commandRepository, unitOfWork);

            Article article = commandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Result result = await useCase.ExecuteAsync(new UpdateArticleStockCommand(article.Id.Value, 3));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(8, article.Stock);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDeltaBelowZeroStock_ShouldFailAndNotCommit()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateArticleStockUseCase(commandRepository, unitOfWork);

            Article article = commandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 2).EnsureValue());

            Result result = await useCase.ExecuteAsync(new UpdateArticleStockCommand(article.Id.Value, -5));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(2, article.Stock);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
