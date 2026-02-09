using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Tests.Persistence.Repositories;

namespace OrderManagement.Infrastructure.Tests.Persistence.EntityConfigurations
{
    [TestClass]
    public class ArticleConfigurationTests : RepositoryTestBase
    {
        [TestMethod]
        public void ArticleConfigurationHasCorrectTableMapping()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));

            Assert.IsNotNull(entityType);
            Assert.AreEqual("Articles", entityType!.GetTableName());
        }

        [TestMethod]
        public void ArticleConfigurationIdIsPrimaryKey()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            IKey? primaryKey = entityType!.FindPrimaryKey();

            Assert.IsNotNull(primaryKey);
            Assert.AreEqual(1, primaryKey!.Properties.Count);
            Assert.AreEqual("Id", primaryKey.Properties[0].Name);
        }

        [TestMethod]
        public void ArticleConfigurationIdIsNotGenerated()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            IProperty? property = entityType!.FindProperty("Id");

            Assert.IsNotNull(property);
            Assert.AreEqual(ValueGenerated.Never, property!.ValueGenerated);
        }

        [TestMethod]
        public void ArticleConfigurationArticleNumberMappedAsOwnedType()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            INavigation? navigation = entityType!.FindNavigation("ArticleNumber");

            Assert.IsNotNull(navigation);
            Assert.IsTrue(navigation!.ForeignKey.IsOwnership);

            IEntityType ownedType = navigation.TargetEntityType;
            IProperty? valueProperty = ownedType.FindProperty("Value");

            Assert.IsNotNull(valueProperty);
            Assert.AreEqual("ArticleNumber", valueProperty!.GetColumnName());
            Assert.AreEqual(20, valueProperty.GetMaxLength());
            Assert.IsFalse(valueProperty.IsNullable);
        }

        [TestMethod]
        public void ArticleConfigurationPriceMappedAsOwnedType()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            INavigation? navigation = entityType!.FindNavigation("Price");

            Assert.IsNotNull(navigation);
            Assert.IsTrue(navigation!.ForeignKey.IsOwnership);

            IEntityType ownedType = navigation.TargetEntityType;
            IProperty? amountProperty = ownedType.FindProperty("Amount");
            IProperty? currencyProperty = ownedType.FindProperty("Currency");

            Assert.IsNotNull(amountProperty);
            Assert.AreEqual("PriceAmount", amountProperty!.GetColumnName());
            Assert.AreEqual(18, amountProperty.GetPrecision());
            Assert.AreEqual(2, amountProperty.GetScale());

            Assert.IsNotNull(currencyProperty);
            Assert.AreEqual("PriceCurrency", currencyProperty!.GetColumnName());
            Assert.AreEqual(3, currencyProperty.GetMaxLength());
        }

        [TestMethod]
        public void ArticleConfigurationNameHasCorrectConstraints()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            IProperty? property = entityType!.FindProperty("Name");

            Assert.IsNotNull(property);
            Assert.AreEqual(200, property!.GetMaxLength());
            Assert.IsFalse(property.IsNullable);
        }

        [TestMethod]
        public void ArticleConfigurationStockMapped()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            IProperty? property = entityType!.FindProperty("Stock");

            Assert.IsNotNull(property);
            Assert.AreEqual(typeof(int), property!.ClrType);
        }

        [TestMethod]
        public void ArticleConfigurationVatRateHasCorrectPrecision()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            IProperty? property = entityType!.FindProperty("VatRate");

            Assert.IsNotNull(property);
            Assert.AreEqual(5, property!.GetPrecision());
            Assert.AreEqual(2, property.GetScale());
        }

        [TestMethod]
        public void ArticleConfigurationDescriptionIsOptional()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            IProperty? property = entityType!.FindProperty("Description");

            Assert.IsNotNull(property);
            Assert.IsTrue(property!.IsNullable);
            Assert.AreEqual(500, property.GetMaxLength());
        }

        [TestMethod]
        public void ArticleConfigurationStatusMapped()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            IProperty? property = entityType!.FindProperty("Status");

            Assert.IsNotNull(property);
            Assert.AreEqual(typeof(int), property!.ClrType);
        }

        [TestMethod]
        public void ArticleConfigurationArticleGroupIdMappedCorrectly()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            IProperty? property = entityType!.FindProperty("ArticleGroupId");

            Assert.IsNotNull(property);
            Assert.AreEqual("ArticleGroupId", property!.GetColumnName());
            Assert.IsFalse(property.IsNullable);
        }

        [TestMethod]
        public void ArticleConfigurationHasForeignKeyToArticleGroup()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(Article));
            IForeignKey? foreignKey = entityType!.GetForeignKeys()
                .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == "ArticleGroupId"));

            Assert.IsNotNull(foreignKey);
            Assert.AreEqual(nameof(ArticleGroup), foreignKey!.PrincipalEntityType.ClrType.Name);
            Assert.AreEqual(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        }

        [TestMethod]
        public void ArticleConfigurationCanPersistCompleteArticle()
        {
            SharedKernel.Primitives.Result<ArticleGroup> groupResult = ArticleGroup.Create(
                id: 1,
                name: "Test Group",
                parentGroupId: null);

            Assert.IsTrue(groupResult.IsSuccess);
            _ = Context.ArticleGroups.Add(groupResult.Value!);
            _ = Context.SaveChanges();

            SharedKernel.Primitives.Result<Article> articleResult = Article.Create(
                id: 1,
                articleNr: "ART-001",
                name: "Test Article",
                priceAmount: 99.99m,
                priceCurrency: "CHF",
                groupId: 1,
                stock: 100,
                vatRate: 7.70m,
                description: "Test Description",
                status: 1);

            Assert.IsTrue(articleResult.IsSuccess);
            _ = Context.Articles.Add(articleResult.Value!);
            int saved = Context.SaveChanges();

            Assert.AreEqual(1, saved);

            Context.ChangeTracker.Clear();
            Article? loaded = Context.Articles.FirstOrDefault(a => a.Id == new ArticleId(1));

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Test Article", loaded!.Name);
            Assert.AreEqual("ART-001", loaded.ArticleNumber.Value);
            Assert.AreEqual(99.99m, loaded.Price.Amount);
            Assert.AreEqual("CHF", loaded.Price.Currency);
            Assert.AreEqual(100, loaded.Stock);
            Assert.AreEqual(7.70m, loaded.VatRate);
            Assert.AreEqual("Test Description", loaded.Description);
            Assert.AreEqual(1, loaded.Status);
        }

        [TestMethod]
        public void ArticleConfigurationCanPersistArticleWithNullDescription()
        {
            SharedKernel.Primitives.Result<ArticleGroup> groupResult = ArticleGroup.Create(
                id: 2,
                name: "Test Group 2",
                parentGroupId: null);

            Assert.IsTrue(groupResult.IsSuccess);
            _ = Context.ArticleGroups.Add(groupResult.Value!);
            _ = Context.SaveChanges();

            SharedKernel.Primitives.Result<Article> articleResult = Article.Create(
                id: 2,
                articleNr: "ART-002",
                name: "Article Without Description",
                priceAmount: 49.99m,
                priceCurrency: "EUR",
                groupId: 2,
                stock: 50,
                description: null);

            Assert.IsTrue(articleResult.IsSuccess);
            _ = Context.Articles.Add(articleResult.Value!);
            _ = Context.SaveChanges();

            Context.ChangeTracker.Clear();
            Article? loaded = Context.Articles.FirstOrDefault(a => a.Id == new ArticleId(2));

            Assert.IsNotNull(loaded);
            Assert.IsNull(loaded!.Description);
        }
    }
}
