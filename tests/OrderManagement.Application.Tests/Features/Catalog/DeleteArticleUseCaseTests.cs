using OrderManagement.Application.Features.Catalog.DeleteArticle;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Catalog
{
    [TestClass]
    public sealed class DeleteArticleUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingUnreferencedArticle_ShouldRemoveAndCommit()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteArticleUseCase(commandRepository, orderQueryRepository, unitOfWork);

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
            var orderQueryRepository = new FakeOrderQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteArticleUseCase(commandRepository, orderQueryRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new DeleteArticleCommand(999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithArticleReferencedByOrderLine_ShouldFailWithArticleInUseAndNotRemove()
        {
            var commandRepository = new FakeArticleCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteArticleUseCase(commandRepository, orderQueryRepository, unitOfWork);

            Article article = commandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());

            Order order = Order.Create(
                "ORD-2026-001",
                new CustomerId(1),
                new DateOnly(2026, 9, 1),
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic).EnsureValue();
            order.AddLine(article.Id, article.Name, article.Price, 1).EnsureSuccess();
            _ = orderQueryRepository.Seed(order);

            Result result = await useCase.ExecuteAsync(new DeleteArticleCommand(article.Id.Value));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(DeleteArticleErrorCodes.ArticleInUse, result.Error);
            Assert.AreEqual(0, commandRepository.Removed.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
