using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Application.Features.Orders.SearchArchivedOrders;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Orders;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class SearchArchivedOrdersUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_ShouldQueryWithArchivedScope()
        {
            var repository = new FakeOrderSearchQueryRepository();
            var useCase = new SearchArchivedOrdersUseCase(repository, new FakeTimeProvider(DateTimeOffset.UtcNow));

            _ = await useCase.ExecuteAsync(new SearchArchivedOrdersQuery(null));

            Assert.AreEqual(OrderSearchScope.Archived, repository.CapturedCriteria!.Scope);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldPassSearchTermThrough()
        {
            var repository = new FakeOrderSearchQueryRepository();
            var useCase = new SearchArchivedOrdersUseCase(repository, new FakeTimeProvider(DateTimeOffset.UtcNow));

            _ = await useCase.ExecuteAsync(new SearchArchivedOrdersQuery("CU00001"));

            Assert.AreEqual("CU00001", repository.CapturedCriteria!.SearchTerm);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithPageBelowOne_ShouldClampToOne()
        {
            var repository = new FakeOrderSearchQueryRepository();
            var useCase = new SearchArchivedOrdersUseCase(repository, new FakeTimeProvider(DateTimeOffset.UtcNow));

            _ = await useCase.ExecuteAsync(new SearchArchivedOrdersQuery(null, Page: -3));

            Assert.AreEqual(1, repository.CapturedCriteria!.Page);
        }
    }
}
