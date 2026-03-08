using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Orders.Query
{
    [TestClass]
    public sealed class OrderQueryRepositoryTests : IntegrationTestBase
    {
        private OrderQueryRepository? _repository;
        private Order? _seededOrder;

        private string _currentOrderNumber = string.Empty;
        private const int SeededCustomerInt = 999;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Assert.IsNotNull(DbContext);
            _repository = new OrderQueryRepository(DbContext);

            _currentOrderNumber = $"ORD-2026-{Random.Shared.Next(100, 999)}";

            Address address = Address.Create("Query-Strasse", "10", "8000", "Zürich", "CH").Value!;

            Result<Order> orderResult = Order.Create(
                id: 1,
                orderNumber: _currentOrderNumber,
                customerId: new CustomerId(SeededCustomerInt),
                deliveryAddress: address
            );

            Assert.IsTrue(orderResult.IsSuccess, $"Order creation failed: {orderResult.Error}");
            _seededOrder = orderResult.Value!;

            _ = await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Orders NOCHECK CONSTRAINT ALL");

            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Order> entry = DbContext.Entry(_seededOrder);
            entry.Property(o => o.Id).IsTemporary = true;

            _ = DbContext.Orders.Add(_seededOrder);
            _ = await DbContext.SaveChangesAsync();

            DbContext.Entry(_seededOrder).State = EntityState.Detached;
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (DbContext != null)
            {
                _ = await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Orders");
                _ = await DbContext.SaveChangesAsync();
            }
        }

        [TestMethod]
        public async Task GetByIdAsync_ShouldReturnCorrectOrder()
        {
            // Arrange
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(_seededOrder);

            Order? dbOrder = await DbContext!.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderNumber == _seededOrder.OrderNumber);

            Assert.IsNotNull(dbOrder, "Seed-Daten konnten nicht gefunden werden.");

            // Act
            Order? result = await _repository.GetByIdAsync(dbOrder.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(dbOrder.Id, result.Id);
            Assert.AreEqual(_currentOrderNumber, result.OrderNumber.Value);
        }

        [TestMethod]
        public async Task GetByOrderNumberAsync_ShouldReturnCorrectOrder()
        {
            // Arrange
            Assert.IsNotNull(_repository);
            OrderNumber orderNumber = OrderNumber.Create(_currentOrderNumber).Value!;

            // Act
            Order? result = await _repository.GetByOrderNumberAsync(orderNumber);

            // Assert
            Assert.IsNotNull(result, $"Order {_currentOrderNumber} wurde nicht gefunden.");
            Assert.AreEqual(_currentOrderNumber, result.OrderNumber.Value);
        }

        [TestMethod]
        public async Task GetByCustomerIdAsync_ShouldReturnOrders()
        {
            // Arrange
            Assert.IsNotNull(_repository);
            var customerId = new CustomerId(SeededCustomerInt);

            // Act
            IReadOnlyList<Order> result = await _repository.GetByCustomerIdAsync(customerId);

            // Assert
            var resultList = result.ToList();
            Assert.IsTrue(resultList.Any(o => o.OrderNumber.Value == _currentOrderNumber));
        }

        [TestMethod]
        public async Task GetListAsync_ShouldReturnAllOrders()
        {
            // Act
            IReadOnlyList<Order> result = await _repository!.GetListAsync();

            // Assert
            Assert.IsTrue(result.Any(o => o.OrderNumber.Value == _currentOrderNumber));
        }

        [TestMethod]
        public async Task GetPendingOrdersAsync_ShouldReturnOrders()
        {
            // Act
            IReadOnlyList<Order> result = await _repository!.GetPendingOrdersAsync();

            // Assert
            Assert.IsTrue(result.Any(o => o.OrderNumber.Value == _currentOrderNumber));
        }

        [TestMethod]
        public async Task GetByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // Act
            Order? result = await _repository!.GetByIdAsync(new OrderId(999999));

            // Assert
            Assert.IsNull(result);
        }
    }
}
