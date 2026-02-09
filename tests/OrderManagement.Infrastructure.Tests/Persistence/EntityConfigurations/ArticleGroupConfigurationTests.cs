using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Tests.Persistence.Repositories;

namespace OrderManagement.Infrastructure.Tests.Persistence.EntityConfigurations
{
    [TestClass]
    public class ArticleGroupConfigurationTests : RepositoryTestBase
    {
        [TestMethod]
        public void ArticleGroupConfigurationHasCorrectTableMapping()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(ArticleGroup));

            Assert.IsNotNull(entityType);
            Assert.AreEqual("ArticleGroups", entityType!.GetTableName());
        }

        [TestMethod]
        public void ArticleGroupConfigurationIdIsPrimaryKey()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(ArticleGroup));
            IKey? primaryKey = entityType!.FindPrimaryKey();

            Assert.IsNotNull(primaryKey);
            Assert.AreEqual("Id", primaryKey!.Properties[0].Name);
        }

        [TestMethod]
        public void ArticleGroupConfigurationNameHasCorrectConstraints()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(ArticleGroup));
            IProperty? property = entityType!.FindProperty("Name");

            Assert.IsNotNull(property);
            Assert.AreEqual(150, property!.GetMaxLength());
            Assert.IsFalse(property.IsNullable);
        }

        [TestMethod]
        public void ArticleGroupConfigurationParentGroupIdIsNullable()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(ArticleGroup));
            IProperty? property = entityType!.FindProperty("ParentGroupId");

            Assert.IsNotNull(property);
            Assert.IsTrue(property!.IsNullable);
        }

        [TestMethod]
        public void ArticleGroupConfigurationHasSelfReferencingForeignKey()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(ArticleGroup));
            IForeignKey? foreignKey = entityType!.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType == entityType);

            Assert.IsNotNull(foreignKey);
            Assert.AreEqual("ParentGroupId", foreignKey!.Properties[0].Name);
        }

        [TestMethod]
        public void ArticleGroupConfigurationChildrenNavigationExists()
        {
            IEntityType? entityType = Context.Model.FindEntityType(typeof(ArticleGroup));
            INavigation? navigation = entityType!.FindNavigation("Children");

            Assert.IsNotNull(navigation);
            Assert.IsTrue(navigation!.IsCollection);
        }

        [TestMethod]
        public void ArticleGroupConfigurationCanPersistHierarchy()
        {
            SharedKernel.Primitives.Result<ArticleGroup> parentResult = ArticleGroup.Create(
                id: 100,
                name: "Parent",
                parentGroupId: null);

            SharedKernel.Primitives.Result<ArticleGroup> childResult = ArticleGroup.Create(
                id: 101,
                name: "Child",
                parentGroupId: 100);

            Assert.IsTrue(parentResult.IsSuccess);
            Assert.IsTrue(childResult.IsSuccess);

            Context.ArticleGroups.AddRange(parentResult.Value!, childResult.Value!);
            int saved = Context.SaveChanges();

            Assert.AreEqual(2, saved);

            ArticleGroup? loaded = Context.ArticleGroups
                .Include(g => g.Children)
                .FirstOrDefault(g => g.Id == new ArticleGroupId(100));

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded!.Children.Count);
            Assert.AreEqual("Child", loaded.Children.First().Name);
        }
    }
}
