using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Application.Features.Orders.SearchActiveOrders;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class SearchActiveOrdersUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_ShouldQueryWithActiveScope()
        {
            var repository = new FakeOrderSearchQueryRepository();
            var useCase = new SearchActiveOrdersUseCase(repository, new FakeTimeProvider(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero)));

            _ = await useCase.ExecuteAsync(new SearchActiveOrdersQuery(null));

            Assert.AreEqual(OrderSearchScope.Active, repository.CapturedCriteria!.Scope);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldPassSearchTermThrough()
        {
            var repository = new FakeOrderSearchQueryRepository();
            var useCase = new SearchActiveOrdersUseCase(repository, new FakeTimeProvider(DateTimeOffset.UtcNow));

            _ = await useCase.ExecuteAsync(new SearchActiveOrdersQuery("ORD-2026-001"));

            Assert.AreEqual("ORD-2026-001", repository.CapturedCriteria!.SearchTerm);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldDeriveTodayFromTimeProvider()
        {
            var repository = new FakeOrderSearchQueryRepository();
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 7, 23, 30, 0, TimeSpan.Zero));
            var useCase = new SearchActiveOrdersUseCase(repository, timeProvider);

            _ = await useCase.ExecuteAsync(new SearchActiveOrdersQuery(null));

            Assert.AreEqual(new DateOnly(2026, 9, 7), repository.CapturedCriteria!.Today);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithPageBelowOne_ShouldClampToOne()
        {
            var repository = new FakeOrderSearchQueryRepository();
            var useCase = new SearchActiveOrdersUseCase(repository, new FakeTimeProvider(DateTimeOffset.UtcNow));

            _ = await useCase.ExecuteAsync(new SearchActiveOrdersQuery(null, Page: 0));

            Assert.AreEqual(1, repository.CapturedCriteria!.Page);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithExcessivePageSize_ShouldClampToMaximum()
        {
            var repository = new FakeOrderSearchQueryRepository();
            var useCase = new SearchActiveOrdersUseCase(repository, new FakeTimeProvider(DateTimeOffset.UtcNow));

            _ = await useCase.ExecuteAsync(new SearchActiveOrdersQuery(null, PageSize: 10_000));

            Assert.AreEqual(100, repository.CapturedCriteria!.PageSize);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldReturnTheRepositoryResult()
        {
            var repository = new FakeOrderSearchQueryRepository
            {
                ResultToReturn = new OrderSearchResultDto([], 5, 1, 15, 1),
            };
            var useCase = new SearchActiveOrdersUseCase(repository, new FakeTimeProvider(DateTimeOffset.UtcNow));

            Result<OrderSearchResultDto> result = await useCase.ExecuteAsync(new SearchActiveOrdersQuery(null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, result.Value!.TotalCount);
        }
    }
}
