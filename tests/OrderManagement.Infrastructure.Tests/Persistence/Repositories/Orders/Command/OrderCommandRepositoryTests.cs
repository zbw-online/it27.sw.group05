using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Command;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Orders.Command
{
    [TestClass]
    public sealed class OrderCommandRepositoryTests : IntegrationTestBase
    {
        private OrderCommandRepository? _repository;
        private const int SharedCustomerId = 999;

        [TestInitialize]
        public async Task Setup()
        {
            Assert.IsNotNull(DbContext);
            _repository = new OrderCommandRepository(DbContext);

            await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Orders");
            await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Customers");

            await DbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO Customers (CustomerId, CustomerNumber, LastName, SurName, Email, PasswordHash) " +
                "VALUES ({0}, 'C-999', 'System', 'Seed', 'seed@test.local', 'hash')", SharedCustomerId);
        }

        [TestMethod]
        public async Task Add_ShouldPersistOrderWithDetails()
        {
            // Arrange
            const int id = 20_001;
            const string orderNr = "ORD-2026-001";

            var address = Address.Create("Main St", "1", "8000", "Zurich", "CH").Value!;

            Order order = Order.Create(
                id: id,
                orderNumber: orderNr,
                customerId: new CustomerId(SharedCustomerId),
                deliveryAddress: address
            ).Value!;

            // Act
            _repository!.Add(order);
            await DbContext.SaveChangesAsync();

            // Assert
            Order? retrieved = await DbContext.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == new OrderId(id));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(orderNr, retrieved.OrderNumber.Value);
        }

        [TestMethod]
        public async Task Update_ShouldModifyOrderDate()
        {
            // Arrange
            const int id = 20_002;
            var address = Address.Create("Test St", "2", "8000", "Zurich", "CH").Value!;
            Order order = Order.Create(id, "ORD-2026-002", new CustomerId(SharedCustomerId), address).Value!;

            DbContext!.Orders.Add(order);
            await DbContext.SaveChangesAsync();
            DbContext.Entry(order).State = EntityState.Detached;

            // Act
            Order? tracked = await DbContext.Orders.FirstOrDefaultAsync(o => o.Id == new OrderId(id));
            Assert.IsNotNull(tracked);

            var newDate = DateTime.UtcNow.AddDays(10);
            typeof(Order).GetProperty(nameof(Order.OrderDate))?.SetValue(tracked, newDate);

            _repository!.Update(tracked);
            await DbContext.SaveChangesAsync();

            // Assert
            Order? updated = await DbContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == new OrderId(id));
            Assert.IsNotNull(updated);
            Assert.AreEqual(newDate.Date, updated.OrderDate.Date);
        }

        [TestMethod]
        public async Task Remove_ShouldDeleteOrder()
        {
            // Arrange
            const int id = 20_003;
            var address = Address.Create("Delete St", "3", "8000", "Zurich", "CH").Value!;
            Order order = Order.Create(id, "ORD-2026-003", new CustomerId(SharedCustomerId), address).Value!;

            DbContext!.Orders.Add(order);
            await DbContext.SaveChangesAsync();

            // Act
            _repository!.Remove(order);
            await DbContext.SaveChangesAsync();

            // Assert
            Order? deleted = await DbContext.Orders.FirstOrDefaultAsync(o => o.Id == new OrderId(id));
            Assert.IsNull(deleted);
        }
    }
}
