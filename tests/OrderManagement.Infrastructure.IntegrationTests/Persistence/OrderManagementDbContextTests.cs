using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Infrastructure.IntegrationTests.Persistence
{
    [TestClass]
    public sealed class OrderManagementDbContextTests : IntegrationTestBase
    {
        [TestMethod]
        public async Task SaveChangesAsync_WithArticleGroup_ShouldPersistAndGenerateTechnicalId()
        {
            ArticleGroup group = ArticleGroup.Create("DbContext Group").EnsureValue();

            _ = DbContext.ArticleGroups.Add(group);
            _ = await DbContext.SaveChangesAsync();

            Assert.IsTrue(group.Id.IsAssigned);

            DbContext.ChangeTracker.Clear();

            ArticleGroup? persisted = await DbContext.ArticleGroups
                .AsNoTracking()
                .SingleOrDefaultAsync(g => g.Id == group.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual("DbContext Group", persisted.Name);
        }

        [TestMethod]
        public async Task SaveChangesAsync_WithOrderAggregate_ShouldPersistOwnedTypesAndChildLines()
        {
            Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);
            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, priceAmount: 12.50m);

            Order order = Order.Create(
                "ORD-2026-401",
                customer.Id,
                new DateOnly(2026, 9, 1),
                InfrastructureTestDataFactory.CreateValidAddress(),
                AddressSource.Automatic,
                InfrastructureTestDataFactory.CreateValidAddress(),
                AddressSource.Automatic).EnsureValue();

            order.AddLine(article.Id, article.Name, article.Price, 2).EnsureSuccess();

            _ = DbContext.Orders.Add(order);
            _ = await DbContext.SaveChangesAsync();

            Assert.IsTrue(order.Id.IsAssigned);

            DbContext.ChangeTracker.Clear();

            Order? persisted = await DbContext.Orders
                .Include(o => o.Lines)
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.Id == order.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual("Main St", persisted.DeliveryAddress.Street);
            Assert.AreEqual(25.00m, persisted.Total.Amount);
            Assert.AreEqual(1, persisted.Lines.Count);
            Assert.IsTrue(persisted.Lines.Single().Id.IsAssigned);
        }

        [TestMethod]
        public void Model_ShouldMapAggregateRootsWithGeneratedKeys()
        {
            IEntityType? customer = DbContext.Model.FindEntityType(typeof(Customer));
            IEntityType? articleGroup = DbContext.Model.FindEntityType(typeof(ArticleGroup));
            IEntityType? article = DbContext.Model.FindEntityType(typeof(Article));
            IEntityType? order = DbContext.Model.FindEntityType(typeof(Order));
            IEntityType? orderLine = DbContext.Model.FindEntityType(typeof(OrderLine));

            AssertGenerated(customer, "Id");
            AssertGenerated(articleGroup, "Id");
            AssertGenerated(article, "Id");
            AssertGenerated(order, "Id");
            AssertGenerated(orderLine, "Id");
        }

        private static void AssertGenerated(IEntityType? entityType, string propertyName)
        {
            Assert.IsNotNull(entityType);

            IProperty? property = entityType.FindProperty(propertyName);
            Assert.IsNotNull(property);
            Assert.AreEqual(ValueGenerated.OnAdd, property.ValueGenerated);
        }
    }
}
