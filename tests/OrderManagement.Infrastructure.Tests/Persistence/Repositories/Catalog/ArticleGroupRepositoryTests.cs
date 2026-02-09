using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog
{
    [TestClass]
    public class ArticleGroupRepositoryTests : RepositoryTestBase
    {
        private readonly ArticleGroupRepository _sut;
        private readonly ArticleGroup? _rootGroup;
        private readonly ArticleGroup? _childGroup1;
        private readonly ArticleGroup? _childGroup2;

        public ArticleGroupRepositoryTests()
        {
            SharedKernel.Primitives.Result<ArticleGroup> rootResult = ArticleGroup.Create(
                id: 1,
                name: "Electronics",
                parentGroupId: null);

            SharedKernel.Primitives.Result<ArticleGroup> childResult1 = ArticleGroup.Create(
                id: 2,
                name: "Computers",
                parentGroupId: 1);

            SharedKernel.Primitives.Result<ArticleGroup> childResult2 = ArticleGroup.Create(
                id: 3,
                name: "Smartphones",
                parentGroupId: 1);

            Assert.IsTrue(rootResult.IsSuccess);
            Assert.IsTrue(childResult1.IsSuccess);
            Assert.IsTrue(childResult2.IsSuccess);

            _rootGroup = rootResult.Value;
            _childGroup1 = childResult1.Value;
            _childGroup2 = childResult2.Value;

            Context.ArticleGroups.AddRange(_rootGroup!, _childGroup1!, _childGroup2!);
            _ = Context.SaveChanges();

            _sut = new ArticleGroupRepository(Context);
        }

        [TestMethod]
        public async Task GetByIdAsyncReturnsCorrectGroup()
        {
            ArticleGroup? result = await _sut.GetByIdAsync(_rootGroup!.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual("Electronics", result.Name);
            Assert.IsNull(result.ParentGroupId);
        }

        [TestMethod]
        public async Task GetByIdAsyncNotFoundReturnsNull()
        {
            ArticleGroup? result = await _sut.GetByIdAsync(new ArticleGroupId(999));

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetListAsyncReturnsAllGroups()
        {
            IReadOnlyList<ArticleGroup> result = await _sut.GetListAsync();

            Assert.AreEqual(3, result.Count);
        }

        [TestMethod]
        public async Task GetByIdWithChildrenAsyncLoadsChildren()
        {
            ArticleGroup? result = await _sut.GetByIdWithChildrenAsync(_rootGroup!.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual("Electronics", result.Name);
            Assert.AreEqual(2, result.Children.Count);
        }

        [TestMethod]
        public async Task GetByIdWithChildrenAsyncNotFoundReturnsNull()
        {
            ArticleGroup? result = await _sut.GetByIdWithChildrenAsync(new ArticleGroupId(999));

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetByParentAsyncReturnsChildGroups()
        {
            IReadOnlyList<ArticleGroup> result = await _sut.GetByParentAsync(_rootGroup!.Id);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(g => g.Name == "Computers"));
            Assert.IsTrue(result.Any(g => g.Name == "Smartphones"));
        }

        [TestMethod]
        public async Task GetByParentAsyncWithNullReturnsRootGroups()
        {
            IReadOnlyList<ArticleGroup> result = await _sut.GetByParentAsync(null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Electronics", result[0].Name);
        }

        [TestMethod]
        public async Task GetByParentAsyncNoChildrenReturnsEmpty()
        {
            IReadOnlyList<ArticleGroup> result = await _sut.GetByParentAsync(_childGroup1!.Id);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void AddSavesNewGroup()
        {
            SharedKernel.Primitives.Result<ArticleGroup> createResult = ArticleGroup.Create(
                id: 4,
                name: "Tablets",
                parentGroupId: 1);

            Assert.IsTrue(createResult.IsSuccess);
            ArticleGroup newGroup = createResult.Value!;

            _sut.Add(newGroup);
            _ = Context.SaveChanges();

            Assert.AreEqual(4, Context.ArticleGroups.Count());

            ArticleGroup? saved = Context.ArticleGroups.FirstOrDefault(g => g.Id == new ArticleGroupId(4));
            Assert.IsNotNull(saved);
            Assert.AreEqual("Tablets", saved!.Name);
            Assert.AreEqual(1, saved.ParentGroupId!.Value.Value);
        }

        [TestMethod]
        public void UpdateUpdatesExisting()
        {
            ArticleGroup group = Context.ArticleGroups.First();
            SharedKernel.Primitives.Result result = group.Rename("Updated Electronics");
            Assert.IsTrue(result.IsSuccess);

            _sut.Update(group);
            _ = Context.SaveChanges();

            ArticleGroup updated = Context.ArticleGroups.First();
            Assert.AreEqual("Updated Electronics", updated.Name);
        }

        [TestMethod]
        public void RemoveRemovesGroup()
        {
            ArticleGroup group = Context.ArticleGroups.First(g => g.Id == _childGroup2!.Id);
            _sut.Remove(group);
            _ = Context.SaveChanges();

            Assert.AreEqual(2, Context.ArticleGroups.Count());
        }
    }
}
