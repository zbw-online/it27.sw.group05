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
        private const int TestCustomerId = 999;

        [TestInitialize]
        public async Task Setup()
        {
            Assert.IsNotNull(DbContext);
            _repository = new OrderQueryRepository(DbContext);

            // Cleanup & Seed Customer
            _ = await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Orders");
            _ = await DbContext.Database.ExecuteSqlRawAsync(
                "IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = {0}) " +
                "INSERT INTO Customers (CustomerId, CustomerNumber, LastName, SurName, Email, PasswordHash) " +
                "VALUES ({0}, 'C-999', 'Query', 'User', 'q@test.com', 'hash')", TestCustomerId);
        }

        private async Task SeedOrderAsync(int id, int sequence)
        {
            string orderNr = $"ORD-2026-{sequence:D3}";

            Address address = Address.Create("Query St", "10", "8000", "Zurich", "CH").Value!;
            Order order = Order.Create(id, orderNr, new CustomerId(TestCustomerId), address).Value!;

            _ = DbContext!.Orders.Add(order);
            _ = await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();
        }

        [TestMethod]
        public async Task GetById_ShouldReturnCorrectOrder()
        {
            // Arrange
            int id = 50001;
            await SeedOrderAsync(id, 1);

            // Act
            Order? result = await _repository!.GetByIdAsync(new OrderId(id));

            // Assert
            Assert.IsNotNull(result, "Repository hat null zurückgegeben.");
            Assert.AreEqual("ORD-2026-001", result.OrderNumber.Value);
        }

        [TestMethod]
        public async Task GetByOrderNumber_ShouldReturnCorrectOrder()
        {
            // Arrange
            int id = 50002;
            await SeedOrderAsync(id, 2);
            OrderNumber vo = OrderNumber.Create("ORD-2026-002").Value!;

            // Act
            Order? result = await _repository!.GetByOrderNumberAsync(vo);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(id, result.Id.Value);
        }
        [TestMethod]
        public async Task GetByCustomerId_ShouldReturnOrdersForSpecificCustomer()
        {
            // Arrange
            int id = 50003;
            await SeedOrderAsync(id, 3);

            // Act
            IReadOnlyList<Order> result = await _repository!.GetByCustomerIdAsync(new CustomerId(TestCustomerId));

            // Assert
            var list = result.ToList();
            Assert.IsTrue(list.Any(o => o.Id == new OrderId(id)), "Die geseedete Order wurde für den Kunden nicht gefunden.");
        }

        [TestMethod]
        public async Task GetListAsync_ShouldReturnAllOrders()
        {
            // Arrange
            await SeedOrderAsync(50004, 4);
            await SeedOrderAsync(50005, 5);

            // Act
            IReadOnlyList<Order> result = await _repository!.GetListAsync();

            // Assert
            Assert.IsTrue(result.Count >= 2, "Es sollten mindestens die zwei geseedeten Orders zurückgegeben werden.");
        }

        [TestMethod]
        public async Task GetById_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            // Act
            Order? result = await _repository!.GetByIdAsync(new OrderId(999999));

            // Assert
            Assert.IsNull(result, "Repository sollte null für eine nicht existierende ID zurückgeben.");
        }
    }
}
