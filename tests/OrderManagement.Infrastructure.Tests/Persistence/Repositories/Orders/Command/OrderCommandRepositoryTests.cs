using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Command;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Orders.Command
{
    [TestClass]
    public sealed class OrderCommandRepositoryTests : IntegrationTestBase
    {
        private OrderCommandRepository? _repository;

        [TestInitialize]
        public async Task Setup()
        {
            Assert.IsNotNull(DbContext);
            _repository = new OrderCommandRepository(DbContext);

            await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Orders");
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (DbContext != null)
            {
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Orders");
                await DbContext.SaveChangesAsync();
            }
        }

        [TestMethod]
        public async Task Add_ShouldPersistOrderToDatabase()
        {

            const string orderNo = "ORD-2026-001";
            Address address = Address.Create("Musterstraße", "12", "12345", "Berlin", "DE").Value!;
            Order order = Order.Create(101, orderNo, new CustomerId(999), address).Value!;

            var entry = DbContext!.Entry(order);
            entry.Property(o => o.Id).IsTemporary = true;

            // Act
            await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Orders NOCHECK CONSTRAINT ALL");
            _repository!.Add(order);
            await DbContext.SaveChangesAsync();

            // Assert
            Order? retrieved = await DbContext.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(orderNo, retrieved.OrderNumber.Value);
        }

        [TestMethod]
        public async Task Update_ShouldModifyExistingOrder()
        {
            // Arrange
            const string orderNo = "ORD-2026-002";
            Address address = Address.Create("Testweg", "1", "12345", "Berlin", "DE").Value!;
            Order order = Order.Create(201, orderNo, new CustomerId(999), address).Value!;

            await DbContext!.Database.ExecuteSqlRawAsync("ALTER TABLE Orders NOCHECK CONSTRAINT ALL");
            var entry = DbContext.Entry(order);
            entry.Property(o => o.Id).IsTemporary = true;

            _repository!.Add(order);
            await DbContext.SaveChangesAsync();
            DbContext.Entry(order).State = EntityState.Detached;

            // Act
            Order? toUpdate = await DbContext.Orders.FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);
            Assert.IsNotNull(toUpdate);

            DateTime updatedDate = DateTime.UtcNow.AddDays(5);
            typeof(Order).GetProperty(nameof(Order.OrderDate))?.SetValue(toUpdate, updatedDate);

            _repository.Update(toUpdate);
            await DbContext.SaveChangesAsync();

            // Assert
            Order? final = await DbContext.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);
            Assert.AreEqual(updatedDate.Date, final!.OrderDate.Date);
        }

        [TestMethod]
        public async Task Remove_ShouldDeleteOrderFromDatabase()
        {
            // Arrange
            const string orderNo = "ORD-2026-003";
            Address address = Address.Create("Löschweg", "404", "00000", "Ex-City", "DE").Value!;
            Order order = Order.Create(301, orderNo, new CustomerId(999), address).Value!;

            await DbContext!.Database.ExecuteSqlRawAsync("ALTER TABLE Orders NOCHECK CONSTRAINT ALL");
            var entry = DbContext.Entry(order);
            entry.Property(o => o.Id).IsTemporary = true;
            _repository!.Add(order);
            await DbContext.SaveChangesAsync();

            // Act
            _repository.Remove(order);
            await DbContext.SaveChangesAsync();

            // Assert
            bool exists = await DbContext.Orders.AnyAsync(o => o.OrderNumber == order.OrderNumber);
            Assert.IsFalse(exists);
        }
    }
}
