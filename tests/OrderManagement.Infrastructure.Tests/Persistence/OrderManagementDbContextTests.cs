using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence
{
    [TestClass]
    public sealed class OrderManagementDbContextTests : IntegrationTestBase
    {
        [TestMethod]
        public async Task SaveChangesAsync_ShouldPersistChanges()
        {
            Result<ArticleGroup> groupResult = ArticleGroup.Create(500, "Test Group");
            Assert.IsTrue(groupResult.IsSuccess);

            _ = DbContext!.ArticleGroups.Add(groupResult.Value!);
            int result = await DbContext!.SaveChangesAsync();

            Assert.IsTrue(result > 0);

            ArticleGroup? retrieved = await DbContext!.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(500));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Test Group", retrieved.Name);
        }

        [TestMethod]
        public async Task SaveChangesAsync_WithMultipleEntities_ShouldReturnCorrectCount()
        {
            Result<ArticleGroup> group1 = ArticleGroup.Create(501, "Group 1");
            Result<ArticleGroup> group2 = ArticleGroup.Create(502, "Group 2");
            Result<ArticleGroup> group3 = ArticleGroup.Create(503, "Group 3");

            Assert.IsTrue(group1.IsSuccess && group2.IsSuccess && group3.IsSuccess);

            _ = DbContext!.ArticleGroups.Add(group1.Value!);
            _ = DbContext!.ArticleGroups.Add(group2.Value!);
            _ = DbContext!.ArticleGroups.Add(group3.Value!);

            int result = await DbContext!.SaveChangesAsync();

            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public async Task SaveChangesAsync_WithNoChanges_ShouldReturnZero()
        {
            int result = await DbContext!.SaveChangesAsync();

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public async Task ChangeTracker_ShouldTrackAddedEntities()
        {
            Result<ArticleGroup> groupResult = ArticleGroup.Create(504, "Tracked Group");
            Assert.IsTrue(groupResult.IsSuccess);

            _ = DbContext!.ArticleGroups.Add(groupResult.Value!);

            EntityEntry<ArticleGroup> entry = DbContext.Entry(groupResult.Value!);
            Assert.AreEqual(EntityState.Added, entry.State);

            _ = await DbContext.SaveChangesAsync();

            Assert.AreEqual(EntityState.Unchanged, entry.State);
        }

        [TestMethod]
        public async Task ChangeTracker_ShouldTrackModifiedEntities()
        {


            Result<ArticleGroup> groupResult = ArticleGroup.Create(505, "Original Name");
            Assert.IsTrue(groupResult.IsSuccess);
            ArticleGroup group = groupResult.Value!;

            _ = DbContext!.ArticleGroups.Add(group);
            _ = await DbContext!.SaveChangesAsync();

            Result renameResult = group.Rename("Modified Name");
            Assert.IsTrue(renameResult.IsSuccess);

            EntityEntry<ArticleGroup> entry = DbContext!.Entry(group);
            Assert.AreEqual(EntityState.Modified, entry.State);
        }

        [TestMethod]
        public async Task ChangeTracker_ShouldTrackDeletedEntities()
        {


            Result<ArticleGroup> groupResult = ArticleGroup.Create(506, "To Delete");
            Assert.IsTrue(groupResult.IsSuccess);
            ArticleGroup group = groupResult.Value!;

            _ = DbContext!.ArticleGroups.Add(group);
            _ = await DbContext!.SaveChangesAsync();

            _ = DbContext!.ArticleGroups.Remove(group);

            EntityEntry<ArticleGroup> entry = DbContext!.Entry(group);
            Assert.AreEqual(EntityState.Deleted, entry.State);
        }

        [TestMethod]
        public async Task Transaction_ShouldRollbackOnError()
        {


            using IDbContextTransaction transaction = await DbContext!.Database.BeginTransactionAsync();

            Result<ArticleGroup> groupResult = ArticleGroup.Create(507, "Transaction Test");
            Assert.IsTrue(groupResult.IsSuccess);

            _ = DbContext.ArticleGroups.Add(groupResult.Value!);
            _ = await DbContext.SaveChangesAsync();

            await transaction.RollbackAsync();

            ArticleGroup? retrieved = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(507));

            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public async Task Transaction_ShouldCommitSuccessfully()
        {


            using IDbContextTransaction transaction = await DbContext!.Database.BeginTransactionAsync();

            Result<ArticleGroup> groupResult = ArticleGroup.Create(508, "Transaction Commit");
            Assert.IsTrue(groupResult.IsSuccess);

            _ = DbContext.ArticleGroups.Add(groupResult.Value!);
            _ = await DbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            ArticleGroup? retrieved = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(508));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Transaction Commit", retrieved.Name);
        }

        [TestMethod]
        public async Task AsNoTracking_ShouldNotTrackEntities()
        {


            Result<ArticleGroup> groupResult = ArticleGroup.Create(509, "No Track Test");
            Assert.IsTrue(groupResult.IsSuccess);

            _ = DbContext!.ArticleGroups.Add(groupResult.Value!);
            _ = await DbContext.SaveChangesAsync();

            // Detach the entity
            DbContext.Entry(groupResult.Value!).State = EntityState.Detached;

            // Query with AsNoTracking
            ArticleGroup? retrieved = await DbContext.ArticleGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(509));

            Assert.IsNotNull(retrieved);

            // Verify the retrieved entity is not being tracked
            EntityEntry<ArticleGroup>? entry = DbContext.ChangeTracker.Entries<ArticleGroup>()
                .FirstOrDefault(e => e.Entity.Id == new ArticleGroupId(509));

            Assert.IsNull(entry);
        }

        [TestMethod]
        public void Model_ShouldContainArticleEntityConfiguration()
        {


            IEntityType? entityType = DbContext!.Model.FindEntityType(typeof(Article));

            Assert.IsNotNull(entityType);
            Assert.AreEqual("Articles", entityType.GetTableName());
        }

        [TestMethod]
        public void Model_ShouldContainArticleGroupEntityConfiguration()
        {


            IEntityType? entityType = DbContext!.Model.FindEntityType(typeof(ArticleGroup));

            Assert.IsNotNull(entityType);
            Assert.AreEqual("ArticleGroups", entityType.GetTableName());
        }

        [TestMethod]
        public async Task Database_CanConnect_ShouldReturnTrue()
        {


            bool canConnect = await DbContext!.Database.CanConnectAsync();

            Assert.IsTrue(canConnect);
        }

        [TestMethod]
        public void Database_ProviderName_ShouldBeSqlServer()
        {


            string? providerName = DbContext!.Database.ProviderName;

            Assert.IsNotNull(providerName);
            Assert.AreEqual("Microsoft.EntityFrameworkCore.SqlServer", providerName);
        }

        [TestMethod]
        public void Set_Generic_ShouldReturnDbSet()
        {


            DbSet<ArticleGroup> set = DbContext!.Set<ArticleGroup>();

            Assert.AreSame(DbContext.ArticleGroups, set);
        }
    }
}
