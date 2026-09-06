using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Orders.Query
{
    [TestClass]
    public sealed class OrderQueryRepositoryTests : IntegrationTestBase
    {
        private OrderQueryRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new OrderQueryRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task GetByIdAsync_WithExistingOrder_ShouldReturnOrder()
        {
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-301");
            DbContext.ChangeTracker.Clear();

            Order? result = await _repository.GetByIdAsync(order.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(order.Id, result.Id);
            Assert.AreEqual("ORD-2026-301", result.OrderNumber.Value);
        }

        [TestMethod]
        public async Task GetByOrderNumberAsync_WithExistingOrderNumber_ShouldReturnOrder()
        {
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-302");
            OrderNumber number = OrderNumber.Create("ORD-2026-302").EnsureValue();

            DbContext.ChangeTracker.Clear();

            Order? result = await _repository.GetByOrderNumberAsync(number);

            Assert.IsNotNull(result);
            Assert.AreEqual(order.Id, result.Id);
        }

        [TestMethod]
        public async Task GetByCustomerIdAsync_WithExistingOrders_ShouldReturnOnlyOrdersOfCustomer()
        {
            Customer customer1 = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            Customer customer2 = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);

            Order order1 = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, customer1.Id, "ORD-2026-303");
            Order order2 = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, customer1.Id, "ORD-2026-304");
            _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, customer2.Id, "ORD-2026-305");

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Order> result = await _repository.GetByCustomerIdAsync(customer1.Id);

            CollectionAssert.AreEquivalent(
                new[] { order1.Id, order2.Id },
                result.Select(o => o.Id).ToArray());
        }

        [TestMethod]
        public async Task GetListAsync_WithOrders_ShouldReturnAllOrders()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-306");
            _ = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(DbContext, orderNumber: "ORD-2026-307");

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Order> result = await _repository.GetListAsync();

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task GetByIdAsync_WithMissingOrder_ShouldReturnNull()
        {
            Order? result = await _repository.GetByIdAsync(new OrderId(999_999));

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetByIdAsync_WithOrderContainingLines_ShouldIncludeLines()
        {
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithLineAsync(DbContext, orderNumber: "ORD-2026-308");
            DbContext.ChangeTracker.Clear();

            Order? result = await _repository.GetByIdAsync(order.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Lines.Count);
        }

        [TestMethod]
        public async Task GetListAsync_WithOrdersContainingLines_ShouldIncludeLines()
        {
            _ = await InfrastructureTestDataFactory.CreatePersistedOrderWithLineAsync(DbContext, orderNumber: "ORD-2026-309");
            DbContext.ChangeTracker.Clear();

            IReadOnlyList<Order> result = await _repository.GetListAsync();

            Assert.AreEqual(1, result.Single(o => o.OrderNumber.Value == "ORD-2026-309").Lines.Count);
        }
    }
}
