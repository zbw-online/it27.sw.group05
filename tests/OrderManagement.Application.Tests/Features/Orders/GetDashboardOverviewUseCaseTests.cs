using OrderManagement.Application.Features.Orders.GetDashboardOverview;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class GetDashboardOverviewUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithOrdersAndCustomers_ShouldAggregateRealCounts()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero));
            var useCase = new GetDashboardOverviewUseCase(orderQueryRepository, customerQueryRepository, articleQueryRepository, timeProvider);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());
            _ = articleQueryRepository.Seed(ValidArticle("ART-001"));

            Order first = ValidOrder(customer.Id, "ORD-2026-001");
            _ = first.AddLine(new ArticleId(1), "Widget", Money.From(10m, "CHF").EnsureValue(), 2);
            _ = orderQueryRepository.Seed(first);

            Order second = ValidOrder(customer.Id, "ORD-2026-002");
            _ = second.AddLine(new ArticleId(1), "Widget", Money.From(30m, "CHF").EnsureValue(), 1);
            _ = orderQueryRepository.Seed(second);

            Result<DashboardOverviewDto> result = await useCase.ExecuteAsync(new GetDashboardOverviewQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            DashboardOverviewDto dto = result.Value!;
            Assert.AreEqual(2, dto.TotalOrders);
            Assert.AreEqual(1, dto.ActiveCustomers);
            Assert.AreEqual(1, dto.ArticleCount);
            Assert.AreEqual(50m, dto.Revenue);
            Assert.AreEqual(25m, dto.AverageOrderValue);
            Assert.AreEqual("CHF", dto.RevenueCurrency);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldReturnRecentOrdersNewestFirstLimited()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero));
            var useCase = new GetDashboardOverviewUseCase(orderQueryRepository, customerQueryRepository, articleQueryRepository, timeProvider);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            _ = orderQueryRepository.Seed(ValidOrder(customer.Id, "ORD-2026-001"));
            _ = orderQueryRepository.Seed(ValidOrder(customer.Id, "ORD-2026-002"));
            _ = orderQueryRepository.Seed(ValidOrder(customer.Id, "ORD-2026-003"));

            Result<DashboardOverviewDto> result = await useCase.ExecuteAsync(new GetDashboardOverviewQuery(RecentOrdersLimit: 2));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(2, result.Value!.RecentOrders.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldPutCurrentOrdersIntoCurrentMonthTrendBucket()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero));
            var useCase = new GetDashboardOverviewUseCase(orderQueryRepository, customerQueryRepository, articleQueryRepository, timeProvider);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            Order order = ValidOrder(customer.Id, "ORD-2026-001", timeProvider);
            _ = order.AddLine(new ArticleId(1), "Widget", Money.From(40m, "CHF").EnsureValue(), 1);
            _ = orderQueryRepository.Seed(order);

            Result<DashboardOverviewDto> result = await useCase.ExecuteAsync(new GetDashboardOverviewQuery(TrendMonths: 3));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(3, result.Value!.MonthlyTrend.Count);
            MonthlyTrendPointDto currentMonth = result.Value.MonthlyTrend[^1];
            Assert.AreEqual(2026, currentMonth.Year);
            Assert.AreEqual(9, currentMonth.Month);
            Assert.AreEqual(1, currentMonth.OrderCount);
            Assert.AreEqual(40m, currentMonth.Revenue);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldUseTimeProviderRatherThanRealWallClockForTheTrendWindow()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2030, 1, 15, 12, 0, 0, TimeSpan.Zero));
            var useCase = new GetDashboardOverviewUseCase(orderQueryRepository, customerQueryRepository, articleQueryRepository, timeProvider);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());
            _ = orderQueryRepository.Seed(ValidOrder(customer.Id, "ORD-2026-001"));

            Result<DashboardOverviewDto> result = await useCase.ExecuteAsync(new GetDashboardOverviewQuery(TrendMonths: 1));

            Assert.IsTrue(result.IsSuccess, result.Error);
            MonthlyTrendPointDto currentMonth = result.Value!.MonthlyTrend[^1];
            Assert.AreEqual(2030, currentMonth.Year);
            Assert.AreEqual(1, currentMonth.Month);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithOrdersAcrossAYearBoundary_ShouldBucketEachIntoItsOwnMonth()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleQueryRepository = new FakeArticleQueryRepository();
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
            var useCase = new GetDashboardOverviewUseCase(orderQueryRepository, customerQueryRepository, articleQueryRepository, timeProvider);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            var decemberTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 12, 20, 9, 0, 0, TimeSpan.Zero));
            Order decemberOrder = Order.Create(
                "ORD-2025-999",
                customer.Id,
                new DateOnly(2025, 12, 20),
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                timeProvider: decemberTimeProvider).EnsureValue();
            _ = decemberOrder.AddLine(new ArticleId(1), "Widget", Money.From(15m, "CHF").EnsureValue(), 1);
            _ = orderQueryRepository.Seed(decemberOrder);

            Order januaryOrder = Order.Create(
                "ORD-2026-001",
                customer.Id,
                new DateOnly(2026, 1, 5),
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                timeProvider: timeProvider).EnsureValue();
            _ = januaryOrder.AddLine(new ArticleId(1), "Widget", Money.From(25m, "CHF").EnsureValue(), 1);
            _ = orderQueryRepository.Seed(januaryOrder);

            Result<DashboardOverviewDto> result = await useCase.ExecuteAsync(new GetDashboardOverviewQuery(TrendMonths: 2));

            Assert.IsTrue(result.IsSuccess, result.Error);
            MonthlyTrendPointDto december = result.Value!.MonthlyTrend[0];
            MonthlyTrendPointDto january = result.Value.MonthlyTrend[1];
            Assert.AreEqual(2025, december.Year);
            Assert.AreEqual(12, december.Month);
            Assert.AreEqual(1, december.OrderCount);
            Assert.AreEqual(2026, january.Year);
            Assert.AreEqual(1, january.Month);
            Assert.AreEqual(1, january.OrderCount);
        }

        private static Order ValidOrder(CustomerId customerId, string orderNumber, TimeProvider? timeProvider = null)
            => Order.Create(
                orderNumber,
                customerId,
                new DateOnly(2026, 9, 1),
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                timeProvider: timeProvider)
            .EnsureValue();

        private static Article ValidArticle(string articleNumber)
            => Article.Create(articleNumber, "Widget", 10m, "CHF", new ArticleGroupId(1), 100).EnsureValue();
    }
}
