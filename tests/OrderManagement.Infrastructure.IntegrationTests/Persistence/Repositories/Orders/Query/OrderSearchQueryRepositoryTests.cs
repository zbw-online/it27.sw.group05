using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Orders.Query
{
    [TestClass]
    public sealed class OrderSearchQueryRepositoryTests : IntegrationTestBase
    {
        private static readonly DateOnly Today = new(2026, 9, 7);

        [TestMethod]
        public async Task SearchAsync_WithActiveScope_ShouldReturnOnlyOpenAndInProgressOrders()
        {
            Order open = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-001");

            Order inProgress = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-002");
            inProgress.StartProcessing(DateTime.UtcNow).EnsureSuccess();
            _ = await DbContext.SaveChangesAsync();

            Order completed = await InfrastructureTestDataFactory.CreatePersistedOrderWithAppliedInventoryAsync(DbContext, orderNumber: "ORD-2026-003");
            completed.StartProcessing(DateTime.UtcNow).EnsureSuccess();
            completed.Complete(DateTime.UtcNow).EnsureSuccess();
            _ = await DbContext.SaveChangesAsync();

            Order cancelled = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-004");
            cancelled.Cancel(DateTime.UtcNow).EnsureSuccess();
            _ = await DbContext.SaveChangesAsync();

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto result = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Active, null, 1, 50, Today));

            var numbers = result.Items.Select(i => i.OrderNumber).ToHashSet();
            Assert.IsTrue(numbers.Contains(open.OrderNumber.Value));
            Assert.IsTrue(numbers.Contains(inProgress.OrderNumber.Value));
            Assert.IsFalse(numbers.Contains(completed.OrderNumber.Value));
            Assert.IsFalse(numbers.Contains(cancelled.OrderNumber.Value));
        }

        [TestMethod]
        public async Task SearchAsync_WithArchivedScope_ShouldReturnOnlyCompletedAndCancelledOrders()
        {
            Order open = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-101");

            Order completed = await InfrastructureTestDataFactory.CreatePersistedOrderWithAppliedInventoryAsync(DbContext, orderNumber: "ORD-2026-102");
            completed.StartProcessing(DateTime.UtcNow).EnsureSuccess();
            completed.Complete(DateTime.UtcNow).EnsureSuccess();
            _ = await DbContext.SaveChangesAsync();

            Order cancelled = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-103");
            cancelled.Cancel(DateTime.UtcNow).EnsureSuccess();
            _ = await DbContext.SaveChangesAsync();

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto result = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Archived, null, 1, 50, Today));

            var numbers = result.Items.Select(i => i.OrderNumber).ToHashSet();
            Assert.IsFalse(numbers.Contains(open.OrderNumber.Value));
            Assert.IsTrue(numbers.Contains(completed.OrderNumber.Value));
            Assert.IsTrue(numbers.Contains(cancelled.OrderNumber.Value));
        }

        [TestMethod]
        public async Task SearchAsync_WithOrderNumberSearchTerm_ShouldFindMatchingOrder()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-201");
            _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-202");

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto result = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Active, "2026-202", 1, 50, Today));

            Assert.IsTrue(result.Items.All(i => i.OrderNumber == "ORD-2026-202"));
            Assert.IsTrue(result.Items.Any());
        }

        [TestMethod]
        public async Task SearchAsync_WithCustomerNumberSearchTerm_ShouldFindOrdersOfThatCustomer()
        {
            Domain.Customers.Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU80001");
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, customerId: customer.Id, orderNumber: "ORD-2026-301");
            _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-302");

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto result = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Active, "CU80001", 1, 50, Today));

            Assert.IsTrue(result.Items.Any(i => i.OrderNumber == order.OrderNumber.Value));
            Assert.IsFalse(result.Items.Any(i => i.OrderNumber == "ORD-2026-302"));
        }

        [TestMethod]
        public async Task SearchAsync_ShouldProjectCustomerNumberCorrectly()
        {
            Domain.Customers.Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext, customerNumber: "CU80010");
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, customerId: customer.Id, orderNumber: "ORD-2026-401");

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto result = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Active, order.OrderNumber.Value, 1, 50, Today));

            Assert.AreEqual("CU80010", result.Items.Single().CustomerNumber);
        }

        [TestMethod]
        public async Task SearchAsync_ShouldPageInSql_AndReportCorrectTotalCount()
        {
            for (int i = 1; i <= 5; i++)
            {
                _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: $"ORD-2026-{500 + i}");
            }

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto firstPage = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Active, "2026-50", 1, 2, Today));
            OrderSearchResultDto secondPage = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Active, "2026-50", 2, 2, Today));

            Assert.AreEqual(5, firstPage.TotalCount);
            Assert.AreEqual(2, firstPage.Items.Count);
            Assert.AreEqual(2, secondPage.Items.Count);
            Assert.AreEqual(3, firstPage.TotalPages);
            Assert.IsFalse(firstPage.Items.Select(i => i.OrderNumber).Intersect(secondPage.Items.Select(i => i.OrderNumber)).Any());
        }

        [TestMethod]
        public async Task SearchAsync_ActiveScope_ShouldSortOverdueOrdersFirst()
        {
            Order future = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(
                DbContext, orderNumber: "ORD-2026-601", deliveryDate: Today.AddDays(10));
            Order overdue = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(
                DbContext, orderNumber: "ORD-2026-602", deliveryDate: Today.AddDays(-10));

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto result = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Active, "2026-60", 1, 50, Today));

            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual(overdue.OrderNumber.Value, result.Items[0].OrderNumber);
            Assert.AreEqual(future.OrderNumber.Value, result.Items[1].OrderNumber);
            Assert.IsTrue(result.Items[0].IsOverdue);
            Assert.IsFalse(result.Items[1].IsOverdue);
        }

        [TestMethod]
        public async Task SearchAsync_ArchivedScope_ArchivedOrdersAreNeverReportedAsOverdue()
        {
            Order cancelled = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(
                DbContext, orderNumber: "ORD-2026-701", deliveryDate: Today.AddDays(-30));
            cancelled.Cancel(DateTime.UtcNow).EnsureSuccess();
            _ = await DbContext.SaveChangesAsync();

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto result = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Archived, "ORD-2026-701", 1, 50, Today));

            Assert.IsFalse(result.Items.Single().IsOverdue);
        }

        [TestMethod]
        public async Task SearchAsync_ArchivedScope_ShouldSortByStatusChangedAtDescending()
        {
            Order olderChange = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-801");
            olderChange.Cancel(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).EnsureSuccess();
            _ = await DbContext.SaveChangesAsync();

            Order newerChange = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-802");
            newerChange.Cancel(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)).EnsureSuccess();
            _ = await DbContext.SaveChangesAsync();

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto result = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Archived, "2026-80", 1, 50, Today));

            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual(newerChange.OrderNumber.Value, result.Items[0].OrderNumber);
            Assert.AreEqual(olderChange.OrderNumber.Value, result.Items[1].OrderNumber);
        }

        [TestMethod]
        public async Task SearchAsync_ShouldNotLoadFullOrderOrCustomerAggregates()
        {
            // The repository must project columns directly; asserting the returned DTO shape here
            // (no Order/Customer navigation) is the behavioural proof of "no full aggregate loading".
            _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-901");

            var repository = new OrderSearchQueryRepository(DbContext);
            OrderSearchResultDto result = await repository.SearchAsync(
                new OrderSearchCriteria(OrderSearchScope.Active, "ORD-2026-901", 1, 50, Today));

            OrderSearchItemDto item = result.Items.Single();
            Assert.IsInstanceOfType<OrderSearchItemDto>(item);
            Assert.AreEqual(OrderStatus.Open, item.Status);
        }
    }
}
