using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Catalog.Query
{
    [TestClass]
    public class ArticleGroupQueryRepositoryTests : RepositoryTestBase
    {
        private readonly ArticleGroupQueryRepository _sut;
        private readonly ArticleGroup? _rootGroup;
        private readonly ArticleGroup? _childGroup1;
        private readonly ArticleGroup? _childGroup2;

        public ArticleGroupQueryRepositoryTests()
        {
            SharedKernel.Primitives.Result<ArticleGroup> rootResult = ArticleGroup.Create(
                id: 1,
                name: "Electronics",
                parentGroupId: null);

            SharedKernel.Primitives.Result<ArticleGroup> child1Result = ArticleGroup.Create(
                id: 2,
                name: "Computers",
                parentGroupId: 1);

            SharedKernel.Primitives.Result<ArticleGroup> child2Result = ArticleGroup.Create(
                id: 3,
                name: "Mobile Devices",
                parentGroupId: 1);

            Assert.IsTrue(rootResult.IsSuccess);
            Assert.IsTrue(child1Result.IsSuccess);
            Assert.IsTrue(child2Result.IsSuccess);

            _rootGroup = rootResult.Value;
            _childGroup1 = child1Result.Value;
            _childGroup2 = child2Result.Value;

            Context.ArticleGroups.AddRange(_rootGroup!, _childGroup1!, _childGroup2!);
            _ = Context.SaveChanges();

            _sut = new ArticleGroupQueryRepository(Context);
        }

        [TestMethod]
        public async Task GetByIdAsyncReturnsCorrectGroup()
        {
            ArticleGroup? result = await _sut.GetByIdAsync(_rootGroup!.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual("Electronics", result.Name);
            Assert.AreEqual(1, result.Id.Value);
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

            List<string> childNames = [.. result.Children.Select(c => c.Name).OrderBy(n => n)];
            Assert.AreEqual("Computers", childNames[0]);
            Assert.AreEqual("Mobile Devices", childNames[1]);
        }

        [TestMethod]
        public async Task GetByIdWithChildrenAsyncNotFoundReturnsNull()
        {
            ArticleGroup? result = await _sut.GetByIdWithChildrenAsync(new ArticleGroupId(999));

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetByIdWithChildrenAsyncNoChildrenReturnsEmptyCollection()
        {
            ArticleGroup? result = await _sut.GetByIdWithChildrenAsync(_childGroup1!.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual("Computers", result.Name);
            Assert.AreEqual(0, result.Children.Count);
        }

        [TestMethod]
        public async Task GetByParentAsyncReturnsChildrenOfParent()
        {
            IReadOnlyList<ArticleGroup> result = await _sut.GetByParentAsync(_rootGroup!.Id);

            Assert.AreEqual(2, result.Count);
            var names = result.Select(g => g.Name).OrderBy(n => n).ToList();
            Assert.AreEqual("Computers", names[0]);
            Assert.AreEqual("Mobile Devices", names[1]);
        }

        [TestMethod]
        public async Task GetByParentAsyncNullParentReturnsRootGroups()
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
        public async Task GetByParentAsyncNonExistentParentReturnsEmpty()
        {
            IReadOnlyList<ArticleGroup> result = await _sut.GetByParentAsync(new ArticleGroupId(999));

            Assert.AreEqual(0, result.Count);
        }
    }
}
