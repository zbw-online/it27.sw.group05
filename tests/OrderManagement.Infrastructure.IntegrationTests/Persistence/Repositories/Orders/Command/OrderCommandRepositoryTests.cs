using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Command;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence.Repositories.Orders.Command
{
    [TestClass]
    public sealed class OrderCommandRepositoryTests : IntegrationTestBase
    {
        private OrderCommandRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new OrderCommandRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task Add_WithExistingCustomer_ShouldPersistOrderAndGenerateTechnicalId()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);

            Order order = Order.Create(
                orderNumber: "ORD-2026-201",
                customerId: customer.Id,
                deliveryDate: new DateOnly(2026, 9, 1),
                billingAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                billingAddressSource: AddressSource.Automatic,
                deliveryAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                deliveryAddressSource: AddressSource.Automatic).EnsureValue();

            _repository.Add(order);
            _ = await DbContext.SaveChangesAsync();

            OrderId orderId = order.Id;
            Assert.IsTrue(orderId.IsAssigned);

            DbContext.ChangeTracker.Clear();

            Order? persisted = await DbContext.Orders
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.Id == orderId);

            Assert.IsNotNull(persisted);
            Assert.AreEqual("ORD-2026-201", persisted.OrderNumber.Value);
            Assert.AreEqual(customer.Id, persisted.CustomerId);
            Assert.AreEqual(0m, persisted.Total.Amount);
            Assert.AreEqual("CHF", persisted.Total.Currency);
        }

        [TestMethod]
        public async Task Add_WithOrderLine_ShouldPersistCompleteAggregateAndGenerateLineId()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, priceAmount: 12.50m);

            Order order = Order.Create(
                orderNumber: "ORD-2026-202",
                customerId: customer.Id,
                deliveryDate: new DateOnly(2026, 9, 1),
                billingAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                billingAddressSource: AddressSource.Automatic,
                deliveryAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                deliveryAddressSource: AddressSource.Automatic).EnsureValue();

            Result addLineResult = order.AddLine(article.Id, article.Name, article.Price, quantity: 2);
            Assert.IsTrue(addLineResult.IsSuccess, addLineResult.Error);

            _repository.Add(order);
            _ = await DbContext.SaveChangesAsync();

            OrderId orderId = order.Id;
            DbContext.ChangeTracker.Clear();

            Order? persisted = await DbContext.Orders
                .Include(o => o.Lines)
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.Id == orderId);

            Assert.IsNotNull(persisted);
            Assert.AreEqual(1, persisted.Lines.Count);
            Assert.AreEqual(25.00m, persisted.Total.Amount);

            OrderLine line = persisted.Lines.Single();
            Assert.IsTrue(line.Id.IsAssigned);
            Assert.AreEqual(article.Id, line.ArticleId);
            Assert.AreEqual(25.00m, line.LineTotal.Amount);
            Assert.AreEqual("CHF", line.LineTotal.Currency);
        }

        [TestMethod]
        public async Task Update_WithDetachedAggregateAndNewLine_ShouldPersistLineAndRecalculatedTotal()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, priceAmount: 12.50m);

            Order order = Order.Create(
                orderNumber: "ORD-2026-203",
                customerId: customer.Id,
                deliveryDate: new DateOnly(2026, 9, 1),
                billingAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                billingAddressSource: AddressSource.Automatic,
                deliveryAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                deliveryAddressSource: AddressSource.Automatic).EnsureValue();

            _repository.Add(order);
            _ = await DbContext.SaveChangesAsync();

            OrderId orderId = order.Id;
            DbContext.ChangeTracker.Clear();

            Order detached = await DbContext.Orders
                .Include(o => o.Lines)
                .AsNoTracking()
                .SingleAsync(o => o.Id == orderId);

            Result addLineResult = detached.AddLine(article.Id, article.Name, article.Price, quantity: 2);
            Assert.IsTrue(addLineResult.IsSuccess, addLineResult.Error);

            _repository.Update(detached);
            _ = await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();

            Order? updated = await DbContext.Orders
                .Include(o => o.Lines)
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.Id == orderId);

            Assert.IsNotNull(updated);
            Assert.AreEqual(1, updated.Lines.Count);
            Assert.AreEqual(25.00m, updated.Total.Amount);
        }

        [TestMethod]
        public async Task Remove_WithExistingOrderContainingLines_ShouldDeleteOrderAndCascadeDeleteLines()
        {
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithLineAsync(DbContext);
            OrderId orderId = order.Id;

            DbContext.ChangeTracker.Clear();

            Order tracked = await DbContext.Orders
                .Include(o => o.Lines)
                .SingleAsync(o => o.Id == orderId);

            OrderLineId lineId = tracked.Lines.Single().Id;

            _repository.Remove(tracked);
            _ = await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();

            bool orderExists = await DbContext.Orders.AsNoTracking().AnyAsync(o => o.Id == orderId);
            bool lineExists = await DbContext.OrderLines.AsNoTracking().AnyAsync(l => l.Id == lineId);

            Assert.IsFalse(orderExists);
            Assert.IsFalse(lineExists);
        }

        [TestMethod]
        public async Task GetByIdAsync_WithExistingOrder_ShouldReturnTrackedOrderWithLines()
        {
            Order order = await InfrastructureTestDataFactory.CreatePersistedOrderWithLineAsync(DbContext);
            OrderId orderId = order.Id;

            DbContext.ChangeTracker.Clear();

            Order? fetched = await _repository.GetByIdAsync(orderId);

            Assert.IsNotNull(fetched);
            Assert.AreEqual(orderId, fetched.Id);
            Assert.AreEqual(1, fetched.Lines.Count);
        }

        [TestMethod]
        public async Task GetByIdAsync_WithMissingOrder_ShouldReturnNull()
        {
            Order? fetched = await _repository.GetByIdAsync(new OrderId(999_999));

            Assert.IsNull(fetched);
        }

        [TestMethod]
        public async Task Update_AfterRemovingALineFromATrackedOrder_ShouldDeleteOnlyThatLineFromTheDatabase()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            Article firstArticle = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, priceAmount: 10m);
            Article secondArticle = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, priceAmount: 20m);

            Order order = Order.Create(
                orderNumber: "ORD-2026-205",
                customerId: customer.Id,
                deliveryDate: new DateOnly(2026, 9, 1),
                billingAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                billingAddressSource: AddressSource.Automatic,
                deliveryAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                deliveryAddressSource: AddressSource.Automatic).EnsureValue();

            Assert.IsTrue(order.AddLine(firstArticle.Id, firstArticle.Name, firstArticle.Price, quantity: 1).IsSuccess);
            Assert.IsTrue(order.AddLine(secondArticle.Id, secondArticle.Name, secondArticle.Price, quantity: 1).IsSuccess);

            _repository.Add(order);
            _ = await DbContext.SaveChangesAsync();

            OrderId orderId = order.Id;
            DbContext.ChangeTracker.Clear();

            Order? tracked = await _repository.GetByIdAsync(orderId);
            Assert.IsNotNull(tracked);

            OrderLine lineToRemove = tracked.Lines.Single(l => l.ArticleId == firstArticle.Id);
            OrderLineId removedLineId = lineToRemove.Id;
            OrderLineId remainingLineId = tracked.Lines.Single(l => l.ArticleId == secondArticle.Id).Id;

            Result removeResult = tracked.RemoveLine(removedLineId);
            Assert.IsTrue(removeResult.IsSuccess, removeResult.Error);

            _repository.Update(tracked);
            _ = await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();

            Order? reloaded = await DbContext.Orders
                .Include(o => o.Lines)
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.Id == orderId);

            Assert.IsNotNull(reloaded);
            Assert.AreEqual(1, reloaded.Lines.Count);
            Assert.AreEqual(20.00m, reloaded.Total.Amount);

            bool removedLineStillExists = await DbContext.OrderLines.AsNoTracking().AnyAsync(l => l.Id == removedLineId);
            bool remainingLineStillExists = await DbContext.OrderLines.AsNoTracking().AnyAsync(l => l.Id == remainingLineId);

            Assert.IsFalse(removedLineStillExists);
            Assert.IsTrue(remainingLineStillExists);
        }

        [TestMethod]
        public async Task Add_WithUnknownCustomerId_ShouldFailBecauseForeignKeyIsEnforced()
        {
            Order order = Order.Create(
                orderNumber: "ORD-2026-204",
                customerId: new CustomerId(999_999),
                deliveryDate: new DateOnly(2026, 9, 1),
                billingAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                billingAddressSource: AddressSource.Automatic,
                deliveryAddress: InfrastructureTestDataFactory.CreateValidAddress(),
                deliveryAddressSource: AddressSource.Automatic).EnsureValue();

            _repository.Add(order);

            _ = await Assert.ThrowsExceptionAsync<DbUpdateException>(
                async () => await DbContext.SaveChangesAsync());
        }
    }
}
