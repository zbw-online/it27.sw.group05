using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.GetDashboardOverview;
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
            var useCase = new GetDashboardOverviewUseCase(orderQueryRepository, customerQueryRepository, articleQueryRepository);

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
            var useCase = new GetDashboardOverviewUseCase(orderQueryRepository, customerQueryRepository, articleQueryRepository);

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
            var useCase = new GetDashboardOverviewUseCase(orderQueryRepository, customerQueryRepository, articleQueryRepository);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            Order order = ValidOrder(customer.Id, "ORD-2026-001");
            _ = order.AddLine(new ArticleId(1), "Widget", Money.From(40m, "CHF").EnsureValue(), 1);
            _ = orderQueryRepository.Seed(order);

            Result<DashboardOverviewDto> result = await useCase.ExecuteAsync(new GetDashboardOverviewQuery(TrendMonths: 3));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(3, result.Value!.MonthlyTrend.Count);
            MonthlyTrendPointDto currentMonth = result.Value.MonthlyTrend[^1];
            Assert.AreEqual(DateTime.UtcNow.Year, currentMonth.Year);
            Assert.AreEqual(DateTime.UtcNow.Month, currentMonth.Month);
            Assert.AreEqual(1, currentMonth.OrderCount);
            Assert.AreEqual(40m, currentMonth.Revenue);
        }

        private static Order ValidOrder(CustomerId customerId, string orderNumber)
            => Order.Create(
                orderNumber,
                customerId,
                new DateOnly(2026, 9, 1),
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic)
            .EnsureValue();

        private static Article ValidArticle(string articleNumber)
            => Article.Create(articleNumber, "Widget", 10m, "CHF", new ArticleGroupId(1), 100).EnsureValue();
    }
}
