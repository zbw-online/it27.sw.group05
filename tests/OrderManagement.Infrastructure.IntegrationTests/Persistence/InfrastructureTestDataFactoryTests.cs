using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.IntegrationTests
{
    [TestClass]
    public sealed class InfrastructureTestDataFactoryTests : IntegrationTestBase
    {
        [TestMethod]
        public async Task CreatePersistedOrderAsync_WithValueEqualBillingAndDeliveryAddresses_ShouldPersistAndReloadBothOwnedAddresses()
        {
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderAsync(
                DbContext,
                orderNumber: "ORD-2026-901");

            DbContext.ChangeTracker.Clear();

            Order? reloaded = await DbContext.Orders
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.Id == order.Id);

            Assert.IsNotNull(reloaded);
            Assert.AreEqual(order.BillingAddress, reloaded.BillingAddress);
            Assert.AreEqual(order.DeliveryAddress, reloaded.DeliveryAddress);
            Assert.IsFalse(ReferenceEquals(order.BillingAddress, order.DeliveryAddress),
                "Billing and delivery must be distinct address snapshot instances even when their values are equal.");
        }
    }
}
