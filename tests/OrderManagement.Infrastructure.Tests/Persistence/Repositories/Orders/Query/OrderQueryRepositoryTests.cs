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
        private const int SeededCustomerInt = 999;

        [TestInitialize]
        public async Task SetupAsync()
        {
            Assert.IsNotNull(DbContext);
            _repository = new OrderQueryRepository(DbContext);
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

        private async Task SeedOrderAsync(string orderNumber, int id)
        {
            var address = Address.Create("Query-Strasse", "10", "8000", "Zürich", "CH").Value!;
            var order = Order.Create(id, orderNumber, new CustomerId(SeededCustomerInt), address).Value!;

            await DbContext!.Database.ExecuteSqlRawAsync("ALTER TABLE Orders NOCHECK CONSTRAINT ALL");
            var entry = DbContext.Entry(order);
            entry.Property(o => o.Id).IsTemporary = true;

            DbContext.Orders.Add(order);
            await DbContext.SaveChangesAsync();
            DbContext.Entry(order).State = EntityState.Detached;
        }
        [TestMethod]
        public async Task GetByIdAsync_ShouldReturnCorrectOrder()
        {
            const string orderNo = "ORD-2026-101";
            await SeedOrderAsync(orderNo, 101);

            var dbOrder = await DbContext!.Orders
                .AsNoTracking()
                .FirstAsync(o => o.OrderNumber == OrderNumber.Create(orderNo).Value);

            var result = await _repository!.GetByIdAsync(dbOrder.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(orderNo, result.OrderNumber.Value);
        }

        [TestMethod]
        public async Task GetByOrderNumberAsync_ShouldReturnCorrectOrder()
        {
            const string orderNo = "ORD-2026-102";
            await SeedOrderAsync(orderNo, 102);
            var vo = OrderNumber.Create(orderNo).Value!;

            var result = await _repository!.GetByOrderNumberAsync(vo);

            Assert.IsNotNull(result);
            Assert.AreEqual(orderNo, result.OrderNumber.Value);
        }

        [TestMethod]
        public async Task GetByCustomerIdAsync_ShouldReturnOrders()
        {
            const string orderNo = "ORD-2026-103";
            await SeedOrderAsync(orderNo, 103);

            var result = await _repository!.GetByCustomerIdAsync(new CustomerId(SeededCustomerInt));

            Assert.IsTrue(result.Any(o => o.OrderNumber.Value == orderNo));
        }

        [TestMethod]
        public async Task GetListAsync_ShouldReturnAllOrders()
        {
            const string orderNo = "ORD-2026-104";
            await SeedOrderAsync(orderNo, 104);

            var result = await _repository!.GetListAsync();

            Assert.IsTrue(result.Any(o => o.OrderNumber.Value == orderNo));
        }

        [TestMethod]
        public async Task GetPendingOrdersAsync_ShouldReturnOrders()
        {
            const string orderNo = "ORD-2026-105";
            await SeedOrderAsync(orderNo, 105);

            var result = await _repository!.GetPendingOrdersAsync();

            Assert.IsTrue(result.Any(o => o.OrderNumber.Value == orderNo));
        }

        [TestMethod]
        public async Task GetByIdAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            var result = await _repository!.GetByIdAsync(new OrderId(999999));
            Assert.IsNull(result);
        }
    }
}
