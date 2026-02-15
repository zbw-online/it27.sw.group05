using OrderManagement.Domain.Catalog;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog.Command
{
    [TestClass]
    public class ArticleGroupCommandRepositoryTests : RepositoryTestBase
    {
        private readonly ArticleGroupCommandRepository _sut;
        private readonly ArticleGroup? _rootGroup;

        public ArticleGroupCommandRepositoryTests()
        {
            SharedKernel.Primitives.Result<ArticleGroup> rootResult = ArticleGroup.Create(
                id: 1,
                name: "Electronics",
                parentGroupId: null);

            Assert.IsTrue(rootResult.IsSuccess);

            _rootGroup = rootResult.Value;

            _ = Context.ArticleGroups.Add(_rootGroup!);
            _ = Context.SaveChanges();

            _sut = new ArticleGroupCommandRepository(Context);
        }

        [TestMethod]
        public void AddSavesNewArticleGroup()
        {
            SharedKernel.Primitives.Result<ArticleGroup> createResult = ArticleGroup.Create(
                id: 2,
                name: "Computers",
                parentGroupId: 1);

            Assert.IsTrue(createResult.IsSuccess);
            ArticleGroup newGroup = createResult.Value!;

            _sut.Add(newGroup);
            _ = Context.SaveChanges();

            Assert.AreEqual(2, Context.ArticleGroups.Count());

            ArticleGroup? saved = Context.ArticleGroups
                .ToList()
                .FirstOrDefault(g => g.Name == "Computers");
            Assert.IsNotNull(saved);
            Assert.AreEqual("Computers", saved!.Name);
            Assert.AreEqual(2, saved.Id.Value);
            Assert.AreEqual(1, saved.ParentGroupId!.Value.Value);
        }

        [TestMethod]
        public void AddSavesRootGroupWithoutParent()
        {
            SharedKernel.Primitives.Result<ArticleGroup> createResult = ArticleGroup.Create(
                id: 3,
                name: "Furniture",
                parentGroupId: null);

            Assert.IsTrue(createResult.IsSuccess);
            ArticleGroup newGroup = createResult.Value!;

            _sut.Add(newGroup);
            _ = Context.SaveChanges();

            Assert.AreEqual(2, Context.ArticleGroups.Count());

            ArticleGroup? saved = Context.ArticleGroups
                .ToList()
                .FirstOrDefault(g => g.Name == "Furniture");
            Assert.IsNotNull(saved);
            Assert.AreEqual("Furniture", saved!.Name);
            Assert.IsNull(saved.ParentGroupId);
        }

        [TestMethod]
        public void UpdateUpdatesExisting()
        {
            ArticleGroup group = Context.ArticleGroups.First();
            SharedKernel.Primitives.Result result = group.Rename("Consumer Electronics");
            Assert.IsTrue(result.IsSuccess);

            _sut.Update(group);
            _ = Context.SaveChanges();

            ArticleGroup updated = Context.ArticleGroups.First();
            Assert.AreEqual("Consumer Electronics", updated.Name);
        }

        [TestMethod]
        public void RemoveRemovesArticleGroup()
        {
            ArticleGroup group = Context.ArticleGroups.First();
            _sut.Remove(group);
            _ = Context.SaveChanges();

            Assert.AreEqual(0, Context.ArticleGroups.Count());
        }

        [TestMethod]
        public void AddMultipleChildGroupsSavesHierarchy()
        {
            SharedKernel.Primitives.Result<ArticleGroup> child1Result = ArticleGroup.Create(
                id: 2,
                name: "Laptops",
                parentGroupId: 1);

            SharedKernel.Primitives.Result<ArticleGroup> child2Result = ArticleGroup.Create(
                id: 3,
                name: "Tablets",
                parentGroupId: 1);

            Assert.IsTrue(child1Result.IsSuccess);
            Assert.IsTrue(child2Result.IsSuccess);

            _sut.Add(child1Result.Value!);
            _sut.Add(child2Result.Value!);
            _ = Context.SaveChanges();

            Assert.AreEqual(3, Context.ArticleGroups.Count());

            var children = Context.ArticleGroups
                .ToList()
                .Where(g => g.ParentGroupId != null && g.ParentGroupId.Value.Value == 1)
                .ToList();

            Assert.AreEqual(2, children.Count);
            Assert.IsTrue(children.Any(c => c.Name == "Laptops"));
            Assert.IsTrue(children.Any(c => c.Name == "Tablets"));
        }
    }
}
