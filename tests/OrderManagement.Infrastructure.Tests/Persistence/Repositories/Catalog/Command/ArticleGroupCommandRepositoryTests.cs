using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog.Command
{
    [TestClass]
    public sealed class ArticleGroupCommandRepositoryTests : IntegrationTestBase
    {
        private ArticleGroupCommandRepository? _repository;

        [TestInitialize]
        public void Setup()
        {
            Assert.IsNotNull(DbContext);
            _repository = new ArticleGroupCommandRepository(DbContext);
        }

        [TestMethod]
        public async Task Add_ShouldPersistArticleGroupToDatabase()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<ArticleGroup> groupResult = ArticleGroup.Create(
                id: 300,
                name: "Electronics"
            );

            Assert.IsTrue(groupResult.IsSuccess);
            ArticleGroup group = groupResult.Value!;

            _repository.Add(group);
            _ = await DbContext.SaveChangesAsync();

            ArticleGroup? retrieved = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(300));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Electronics", retrieved.Name);
        }

        [TestMethod]
        public async Task Add_WithParentGroup_ShouldPersistWithParentId()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            // Create parent group
            Result<ArticleGroup> parentResult = ArticleGroup.Create(301, "Parent");
            Assert.IsTrue(parentResult.IsSuccess);
            _ = DbContext.ArticleGroups.Add(parentResult.Value!);
            _ = await DbContext.SaveChangesAsync();

            // Create child group
            Result<ArticleGroup> childResult = ArticleGroup.Create(
                id: 302,
                name: "Child Group",
                parentGroupId: 301
            );

            Assert.IsTrue(childResult.IsSuccess);
            ArticleGroup child = childResult.Value!;

            _repository.Add(child);
            _ = await DbContext.SaveChangesAsync();

            ArticleGroup? retrieved = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(302));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Child Group", retrieved.Name);
            Assert.IsNotNull(retrieved.ParentGroupId);
            Assert.AreEqual(301, retrieved.ParentGroupId.Value.Value);
        }

        [TestMethod]
        public async Task Update_ShouldModifyExistingGroup()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<ArticleGroup> groupResult = ArticleGroup.Create(303, "Original Name");
            Assert.IsTrue(groupResult.IsSuccess);
            ArticleGroup group = groupResult.Value!;

            _ = DbContext.ArticleGroups.Add(group);
            _ = await DbContext.SaveChangesAsync();
            DbContext.Entry(group).State = EntityState.Detached;

            ArticleGroup? tracked = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(303));

            Assert.IsNotNull(tracked);

            Result renameResult = tracked.Rename("Updated Name");
            Assert.IsTrue(renameResult.IsSuccess);

            _repository.Update(tracked);
            _ = await DbContext.SaveChangesAsync();

            ArticleGroup? updated = await DbContext.ArticleGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(303));

            Assert.IsNotNull(updated);
            Assert.AreEqual("Updated Name", updated.Name);
        }

        [TestMethod]
        public async Task Remove_ShouldDeleteGroupFromDatabase()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<ArticleGroup> groupResult = ArticleGroup.Create(304, "To Be Deleted");
            Assert.IsTrue(groupResult.IsSuccess);
            ArticleGroup group = groupResult.Value!;

            _ = DbContext.ArticleGroups.Add(group);
            _ = await DbContext.SaveChangesAsync();

            _repository.Remove(group);
            _ = await DbContext.SaveChangesAsync();

            ArticleGroup? deleted = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(304));

            Assert.IsNull(deleted);
        }

        [TestMethod]
        public async Task Add_MultipleGroups_ShouldPersistAll()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            for (int i = 305; i <= 309; i++)
            {
                Result<ArticleGroup> result = ArticleGroup.Create(
                    id: i,
                    name: $"Group {i}"
                );

                Assert.IsTrue(result.IsSuccess);
                _repository.Add(result.Value!);
            }

            _ = await DbContext.SaveChangesAsync();

            int count = await DbContext.ArticleGroups.CountAsync();
            Assert.AreEqual(5, count);
        }

        [TestMethod]
        public async Task Update_GroupWithChildren_ShouldMaintainHierarchy()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            // Create parent and child
            Result<ArticleGroup> parentResult = ArticleGroup.Create(1, "Parent");
            Result<ArticleGroup> childResult = ArticleGroup.Create(2, "Child", parentGroupId: 1);

            Assert.IsTrue(parentResult.IsSuccess && childResult.IsSuccess);

            ArticleGroup parent = parentResult.Value!;
            ArticleGroup child = childResult.Value!;

            _ = DbContext.ArticleGroups.Add(parent);
            _ = DbContext.ArticleGroups.Add(child);
            _ = await DbContext.SaveChangesAsync();
            DbContext.Entry(parent).State = EntityState.Detached;

            // Update parent
            ArticleGroup? trackedParent = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(1));

            Assert.IsNotNull(trackedParent);

            Result renameResult = trackedParent.Rename("Updated Parent");
            Assert.IsTrue(renameResult.IsSuccess);

            _repository.Update(trackedParent);
            _ = await DbContext.SaveChangesAsync();

            // Verify child still references parent
            ArticleGroup? retrievedChild = await DbContext.ArticleGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(2));

            Assert.IsNotNull(retrievedChild);
            Assert.IsNotNull(retrievedChild.ParentGroupId);
            Assert.AreEqual(1, retrievedChild.ParentGroupId.Value.Value);

            // Verify parent was updated
            ArticleGroup? retrievedParent = await DbContext.ArticleGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(1));

            Assert.IsNotNull(retrievedParent);
            Assert.AreEqual("Updated Parent", retrievedParent.Name);
        }
    }
}
