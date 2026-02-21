using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog.Query
{
    [TestClass]
    public sealed class ArticleGroupQueryRepositoryTests : IntegrationTestBase
    {
        private ArticleGroupQueryRepository? _repository;

        [TestInitialize]
        public void Setup()
        {
            Assert.IsNotNull(DbContext);
            _repository = new ArticleGroupQueryRepository(DbContext);
        }

        [TestMethod]
        public async Task GetByIdAsync_ExistingGroup_ReturnsGroup()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<ArticleGroup> groupResult = ArticleGroup.Create(400, "Electronics");
            Assert.IsTrue(groupResult.IsSuccess);
            ArticleGroup group = groupResult.Value!;

            _ = DbContext.ArticleGroups.Add(group);
            _ = await DbContext.SaveChangesAsync();

            ArticleGroup? retrieved = await _repository.GetByIdAsync(new ArticleGroupId(400));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Electronics", retrieved.Name);
            Assert.AreEqual(400, retrieved.Id.Value);
        }

        [TestMethod]
        public async Task GetByIdAsync_NonExistingGroup_ReturnsNull()
        {
            Assert.IsNotNull(_repository);

            ArticleGroup? result = await _repository.GetByIdAsync(new ArticleGroupId(999));

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetListAsync_ReturnsAllGroups()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            for (int i = 401; i <= 403; i++)
            {
                Result<ArticleGroup> result = ArticleGroup.Create(i, $"Group {i}");
                Assert.IsTrue(result.IsSuccess);
                _ = DbContext.ArticleGroups.Add(result.Value!);
            }

            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroup> groups = await _repository.GetListAsync();

            Assert.AreEqual(3, groups.Count);
        }

        [TestMethod]
        public async Task GetByIdWithChildrenAsync_IncludesChildren()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<ArticleGroup> parentResult = ArticleGroup.Create(404, "Parent Group");
            Assert.IsTrue(parentResult.IsSuccess);
            ArticleGroup parent = parentResult.Value!;

            Result<ArticleGroup> childResult = ArticleGroup.Create(405, "Child Group", parentGroupId: 404);
            Assert.IsTrue(childResult.IsSuccess);
            ArticleGroup child = childResult.Value!;

            _ = parent.AddChild(child);

            _ = DbContext.ArticleGroups.Add(parent);
            _ = DbContext.ArticleGroups.Add(child);
            _ = await DbContext.SaveChangesAsync();

            ArticleGroup? retrieved = await _repository.GetByIdWithChildrenAsync(new ArticleGroupId(404));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Parent Group", retrieved.Name);
            Assert.AreEqual(1, retrieved.Children.Count);
            Assert.AreEqual("Child Group", retrieved.Children.First().Name);
        }

        [TestMethod]
        public async Task GetByParentAsync_ReturnsChildrenOfParent()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<ArticleGroup> parentResult = ArticleGroup.Create(1, "Parent");
            Result<ArticleGroup> child1Result = ArticleGroup.Create(2, "Child 1", parentGroupId: 1);
            Result<ArticleGroup> child2Result = ArticleGroup.Create(3, "Child 2", parentGroupId: 1);

            Assert.IsTrue(parentResult.IsSuccess && child1Result.IsSuccess && child2Result.IsSuccess);

            _ = DbContext.ArticleGroups.Add(parentResult.Value!);
            _ = DbContext.ArticleGroups.Add(child1Result.Value!);
            _ = DbContext.ArticleGroups.Add(child2Result.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroup> children = await _repository.GetByParentAsync(new ArticleGroupId(1));

            Assert.AreEqual(2, children.Count);
            Assert.IsTrue(children.All(c => c.ParentGroupId?.Value == 1));
        }

        [TestMethod]
        public async Task GetByParentAsync_NullParent_ReturnsRootGroups()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<ArticleGroup> root1 = ArticleGroup.Create(1, "Root 1");
            Result<ArticleGroup> root2 = ArticleGroup.Create(2, "Root 2");
            Result<ArticleGroup> child = ArticleGroup.Create(3, "Child", parentGroupId: 1);

            Assert.IsTrue(root1.IsSuccess && root2.IsSuccess && child.IsSuccess);

            _ = DbContext.ArticleGroups.Add(root1.Value!);
            _ = DbContext.ArticleGroups.Add(root2.Value!);
            _ = DbContext.ArticleGroups.Add(child.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroup> rootGroups = await _repository.GetByParentAsync(null);

            Assert.AreEqual(2, rootGroups.Count);
            Assert.IsTrue(rootGroups.All(g => g.ParentGroupId == null));
        }
    }
}
