using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.DTOs.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog.Query
{
    [TestClass]
    public sealed class ArticleGroupQueryRepositoryTests : IntegrationTestBase
    {
        private ArticleGroupQueryRepository _repository = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _repository = new ArticleGroupQueryRepository(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task GetByIdAsync_WithExistingGroup_ShouldReturnGroup()
        {
            ArticleGroup group = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Electronics");
            DbContext.ChangeTracker.Clear();

            ArticleGroup? result = await _repository.GetByIdAsync(group.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(group.Id, result.Id);
            Assert.AreEqual("Electronics", result.Name);
        }

        [TestMethod]
        public async Task GetByParentAsync_WithParentId_ShouldReturnDirectChildrenOnly()
        {
            ArticleGroup parent = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Parent");
            ArticleGroup child1 = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Child 1", parent.Id);
            ArticleGroup child2 = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Child 2", parent.Id);
            _ = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Other Root");

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<ArticleGroup> result = await _repository.GetByParentAsync(parent.Id);

            CollectionAssert.AreEquivalent(
                new[] { child1.Id, child2.Id },
                result.Select(g => g.Id).ToArray());
        }

        [TestMethod]
        public async Task GetByParentAsync_WithNullParent_ShouldReturnRootGroupsOnly()
        {
            ArticleGroup root = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Root");
            _ = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Child", root.Id);

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<ArticleGroup> result = await _repository.GetByParentAsync(null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(root.Id, result.Single().Id);
        }

        [TestMethod]
        public async Task GetHierarchyFromRootAsync_WithThreeLevels_ShouldReturnRootAndDescendants()
        {
            ArticleGroup root = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Root");
            ArticleGroup child = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Child", root.Id);
            ArticleGroup leaf = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "Leaf", child.Id);

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<ArticleGroupHierarchyDto> result = await _repository.GetHierarchyFromRootAsync(root.Id);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(x => x.Id == root.Id.Value && x.Level == 0 && x.Path == "Root"));
            Assert.IsTrue(result.Any(x => x.Id == child.Id.Value && x.Level == 1 && x.Path == "Root > Child"));
            Assert.IsTrue(result.Any(x => x.Id == leaf.Id.Value && x.Level == 2 && x.Path == "Root > Child > Leaf"));
        }

        [TestMethod]
        public async Task GetFullHierarchyAsync_WithMultipleRoots_ShouldReturnAllHierarchyRows()
        {
            ArticleGroup root1 = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "AAA Root");
            _ = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "BBB Child", root1.Id);
            ArticleGroup root2 = await InfrastructureTestDataFactory.CreatePersistedArticleGroupAsync(DbContext, "ZZZ Root");

            DbContext.ChangeTracker.Clear();

            IReadOnlyList<ArticleGroupHierarchyDto> result = await _repository.GetFullHierarchyAsync();

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(x => x.Id == root1.Id.Value && x.Path == "AAA Root"));
            Assert.IsTrue(result.Any(x => x.Id == root2.Id.Value && x.Path == "ZZZ Root"));
            Assert.IsTrue(result.Any(x => x.Path == "AAA Root > BBB Child"));
        }
    }
}
