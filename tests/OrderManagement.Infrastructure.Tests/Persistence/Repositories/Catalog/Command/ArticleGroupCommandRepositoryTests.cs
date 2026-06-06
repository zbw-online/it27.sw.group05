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
        private ArticleGroupCommandRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new ArticleGroupCommandRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task Add_WithRootGroup_ShouldPersistAndGenerateId()
        {
            ArticleGroup group = ArticleGroup.Create("Electronics").EnsureValue();

            _repository.Add(group);
            _ = await DbContext.SaveChangesAsync();

            Assert.IsTrue(group.Id.IsAssigned);

            DbContext.ChangeTracker.Clear();

            ArticleGroup? persisted = await DbContext.ArticleGroups
                .AsNoTracking()
                .SingleOrDefaultAsync(g => g.Id == group.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual("Electronics", persisted.Name);
            Assert.IsNull(persisted.ParentGroupId);
        }

        [TestMethod]
        public async Task Add_WithPersistedParent_ShouldPersistParentReference()
        {
            ArticleGroup parent = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Parent Group");
            ArticleGroup child = ArticleGroup.Create("Child Group", parent.Id).EnsureValue();

            _repository.Add(child);
            _ = await DbContext.SaveChangesAsync();

            Assert.IsTrue(child.Id.IsAssigned);

            DbContext.ChangeTracker.Clear();

            ArticleGroup? persisted = await DbContext.ArticleGroups
                .AsNoTracking()
                .SingleOrDefaultAsync(g => g.Id == child.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual(parent.Id, persisted.ParentGroupId);
        }

        [TestMethod]
        public async Task Update_WithRenamedGroup_ShouldPersistNewName()
        {
            ArticleGroup group = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Original Name");
            ArticleGroupId groupId = group.Id;

            DbContext.ChangeTracker.Clear();

            ArticleGroup tracked = await DbContext.ArticleGroups.SingleAsync(g => g.Id == groupId);
            Result renameResult = tracked.Rename("Renamed Group");
            Assert.IsTrue(renameResult.IsSuccess, renameResult.Error);

            _repository.Update(tracked);
            _ = await DbContext.SaveChangesAsync();

            DbContext.ChangeTracker.Clear();

            ArticleGroup? persisted = await DbContext.ArticleGroups
                .AsNoTracking()
                .SingleOrDefaultAsync(g => g.Id == groupId);

            Assert.IsNotNull(persisted);
            Assert.AreEqual("Renamed Group", persisted.Name);
        }

        [TestMethod]
        public async Task Remove_WithLeafGroup_ShouldDeleteGroup()
        {
            ArticleGroup group = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Delete Me");
            ArticleGroupId groupId = group.Id;

            DbContext.ChangeTracker.Clear();

            ArticleGroup tracked = await DbContext.ArticleGroups.SingleAsync(g => g.Id == groupId);

            _repository.Remove(tracked);
            _ = await DbContext.SaveChangesAsync();

            bool exists = await DbContext.ArticleGroups.AsNoTracking().AnyAsync(g => g.Id == groupId);

            Assert.IsFalse(exists);
        }
    }
}
