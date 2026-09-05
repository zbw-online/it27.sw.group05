using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.GetTopSellingArticles;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class GetTopSellingArticlesUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_ShouldRankArticlesByTotalQuantityDescending()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var useCase = new GetTopSellingArticlesUseCase(orderQueryRepository, articleQueryRepository);

            Article screw = articleQueryRepository.Seed(
                Article.Create("ART-001", "Schraube M8", 1m, "CHF", new ArticleGroupId(1), 500).EnsureValue());
            Article dowel = articleQueryRepository.Seed(
                Article.Create("ART-002", "Dübel 10mm", 1m, "CHF", new ArticleGroupId(1), 500).EnsureValue());

            Order order = Order.Create("ORD-2026-001", new CustomerId(1), new DateOnly(2026, 9, 1), Address.Create("Main", "1", "8000", "Zurich", "CH").EnsureValue(), AddressSource.Automatic, Address.Create("Main", "1", "8000", "Zurich", "CH").EnsureValue(), AddressSource.Automatic).EnsureValue();
            _ = order.AddLine(screw.Id, screw.Name, screw.Price, 50);
            _ = order.AddLine(dowel.Id, dowel.Name, dowel.Price, 10);
            _ = orderQueryRepository.Seed(order);

            Order secondOrder = Order.Create("ORD-2026-002", new CustomerId(1), new DateOnly(2026, 9, 1), Address.Create("Main", "1", "8000", "Zurich", "CH").EnsureValue(), AddressSource.Automatic, Address.Create("Main", "1", "8000", "Zurich", "CH").EnsureValue(), AddressSource.Automatic).EnsureValue();
            _ = secondOrder.AddLine(screw.Id, screw.Name, screw.Price, 30);
            _ = orderQueryRepository.Seed(secondOrder);

            Result<IReadOnlyList<TopSellingArticleDto>> result = await useCase.ExecuteAsync(new GetTopSellingArticlesQuery(Limit: 2));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(2, result.Value!.Count);
            Assert.AreEqual("ART-001", result.Value[0].ArticleNumber);
            Assert.AreEqual(80, result.Value[0].TotalQuantity);
            Assert.AreEqual(2, result.Value[0].OrderCount);
            Assert.AreEqual("ART-002", result.Value[1].ArticleNumber);
            Assert.AreEqual(10, result.Value[1].TotalQuantity);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithNoOrders_ShouldReturnEmptyList()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var useCase = new GetTopSellingArticlesUseCase(orderQueryRepository, articleQueryRepository);

            Result<IReadOnlyList<TopSellingArticleDto>> result = await useCase.ExecuteAsync(new GetTopSellingArticlesQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(0, result.Value!.Count);
        }
    }
}
