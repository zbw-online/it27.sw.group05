using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Command;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Orders.Command
{
    [TestClass]
    public sealed class OrderCommandRepositoryTests : IntegrationTestBase
    {
        private OrderCommandRepository? _repository;

        [TestInitialize]
        public void Setup()
        {
            Assert.IsNotNull(DbContext);
            _repository = new OrderCommandRepository(DbContext);
        }

        [TestMethod]
        public async Task Add_ShouldPersistOrderToDatabase()
        {
            // Arrange
            Assert.IsNotNull(_repository);
            var address = Address.Create("Musterstraße", "12", "12345", "Berlin", "DE").Value!;

            var orderResult = Order.Create(
                id: 1,
                orderNumber: "ORD-2026-999",
                customerId: new CustomerId(999),
                deliveryAddress: address
            );

            Assert.IsTrue(orderResult.IsSuccess);
            var order = orderResult.Value!;

            var entry = DbContext!.Entry(order);
            entry.Property(o => o.Id).IsTemporary = true;

            // Act
            await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Orders NOCHECK CONSTRAINT ALL");

            try
            {
                _repository.Add(order);
                await DbContext.SaveChangesAsync();
            }
            finally
            {
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Orders CHECK CONSTRAINT ALL");
            }

            // Assert
            var retrieved = await DbContext.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);

            Assert.IsNotNull(retrieved, "Order wurde nicht in der Datenbank gefunden.");
            Assert.AreEqual(order.OrderNumber.Value, retrieved.OrderNumber.Value);
        }

        [TestMethod]
        public async Task Update_ShouldModifyExistingOrder()
        {
            // Arrange
            var address = Address.Create("Testweg", "1", "12345", "Berlin", "DE").Value!;
            var orderResult = Order.Create(201, "ORD-2026-001", new CustomerId(999), address);

            Assert.IsTrue(orderResult.IsSuccess, $"Order.Create fehlgeschlagen: {orderResult.Error}");
            var order = orderResult.Value;
            Assert.IsNotNull(order, "Order Objekt ist null");

            await DbContext!.Database.ExecuteSqlRawAsync("ALTER TABLE Orders NOCHECK CONSTRAINT ALL");

            var entry = DbContext.Entry(order);
            entry.Property(o => o.Id).IsTemporary = true;

            _repository!.Add(order);
            await DbContext.SaveChangesAsync();

            DbContext.Entry(order).State = EntityState.Detached;

            // Act
            var orderToUpdate = await DbContext.Orders
                .FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);

            Assert.IsNotNull(orderToUpdate, "Order zum Updaten wurde nicht gefunden");

            var updatedDate = DateTime.UtcNow.AddDays(5);
            var dateProp = typeof(Order).GetProperty(nameof(Order.OrderDate));
            dateProp?.SetValue(orderToUpdate, updatedDate);

            _repository.Update(orderToUpdate);
            await DbContext.SaveChangesAsync();

            // Assert
            var finalOrder = await DbContext.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);

            Assert.IsNotNull(finalOrder);
            Assert.AreEqual(updatedDate.Date, finalOrder.OrderDate.Date);
        }
        [TestMethod]
        public async Task Remove_ShouldDeleteOrderFromDatabase()
        {
            // Arrange
            var address = Address.Create("Löschweg", "404", "00000", "Ex-City", "DE").Value!;
            var orderResult = Order.Create(301, "ORD-2026-001", new CustomerId(999), address);

            Assert.IsTrue(orderResult.IsSuccess);
            var order = orderResult.Value!;

            await DbContext!.Database.ExecuteSqlRawAsync("ALTER TABLE Orders NOCHECK CONSTRAINT ALL");

            var entry = DbContext.Entry(order);
            entry.Property(o => o.Id).IsTemporary = true;
            _repository!.Add(order);
            await DbContext.SaveChangesAsync();

            var exists = await DbContext.Orders.AnyAsync(o => o.OrderNumber == order.OrderNumber);
            Assert.IsTrue(exists, "Order wurde nicht korrekt initialisiert.");

            // Act
            _repository.Remove(order);
            await DbContext.SaveChangesAsync();

            // Assert
            var deletedOrder = await DbContext.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderNumber == order.OrderNumber);

            Assert.IsNull(deletedOrder, "Die Order sollte gelöscht sein, wurde aber in der DB gefunden.");
        }
    }
}
