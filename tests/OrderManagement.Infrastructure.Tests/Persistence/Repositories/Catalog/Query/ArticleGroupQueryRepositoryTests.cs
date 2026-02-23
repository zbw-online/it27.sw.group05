using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
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

        [TestMethod]
        public async Task GetHierarchyFromRootAsync_SingleLevel_ReturnsOnlyRoot()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<ArticleGroup> rootResult = ArticleGroup.Create(450, "Single Root");
            Assert.IsTrue(rootResult.IsSuccess);
            _ = DbContext.ArticleGroups.Add(rootResult.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetHierarchyFromRootAsync(new ArticleGroupId(450));

            Assert.AreEqual(1, hierarchy.Count);
            Assert.AreEqual(450, hierarchy[0].Id);
            Assert.AreEqual("Single Root", hierarchy[0].Name);
            Assert.AreEqual(0, hierarchy[0].Level);
            Assert.AreEqual("Single Root", hierarchy[0].Path);
            Assert.IsNull(hierarchy[0].ParentGroupId);
        }

        [TestMethod]
        public async Task GetHierarchyFromRootAsync_TwoLevels_ReturnsParentAndChild()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            // Create parent
            Result<ArticleGroup> parentResult = ArticleGroup.Create(451, "Electronics");
            Assert.IsTrue(parentResult.IsSuccess);
            _ = DbContext.ArticleGroups.Add(parentResult.Value!);

            // Create child
            Result<ArticleGroup> childResult = ArticleGroup.Create(452, "Laptops", parentGroupId: 451);
            Assert.IsTrue(childResult.IsSuccess);
            _ = DbContext.ArticleGroups.Add(childResult.Value!);

            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetHierarchyFromRootAsync(new ArticleGroupId(451));

            Assert.AreEqual(2, hierarchy.Count);

            // Verify parent (Level 0)
            ArticleGroupHierarchyDto parent = hierarchy.First(h => h.Id == 451);
            Assert.AreEqual("Electronics", parent.Name);
            Assert.AreEqual(0, parent.Level);
            Assert.AreEqual("Electronics", parent.Path);
            Assert.IsNull(parent.ParentGroupId);

            // Verify child (Level 1)
            ArticleGroupHierarchyDto child = hierarchy.First(h => h.Id == 452);
            Assert.AreEqual("Laptops", child.Name);
            Assert.AreEqual(1, child.Level);
            Assert.AreEqual("Electronics > Laptops", child.Path);
            Assert.AreEqual(451, child.ParentGroupId);
        }

        [TestMethod]
        public async Task GetHierarchyFromRootAsync_ThreeLevels_ReturnsFullHierarchy()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            // Create 3-level hierarchy: Electronics > Computers > Gaming Laptops
            Result<ArticleGroup> level0 = ArticleGroup.Create(453, "Electronics");
            Result<ArticleGroup> level1 = ArticleGroup.Create(454, "Computers", parentGroupId: 453);
            Result<ArticleGroup> level2 = ArticleGroup.Create(455, "Gaming Laptops", parentGroupId: 454);

            Assert.IsTrue(level0.IsSuccess && level1.IsSuccess && level2.IsSuccess);

            _ = DbContext.ArticleGroups.Add(level0.Value!);
            _ = DbContext.ArticleGroups.Add(level1.Value!);
            _ = DbContext.ArticleGroups.Add(level2.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetHierarchyFromRootAsync(new ArticleGroupId(453));

            Assert.AreEqual(3, hierarchy.Count);

            // Verify all levels
            ArticleGroupHierarchyDto root = hierarchy.First(h => h.Level == 0);
            Assert.AreEqual("Electronics", root.Name);
            Assert.AreEqual("Electronics", root.Path);

            ArticleGroupHierarchyDto middle = hierarchy.First(h => h.Level == 1);
            Assert.AreEqual("Computers", middle.Name);
            Assert.AreEqual("Electronics > Computers", middle.Path);
            Assert.AreEqual(453, middle.ParentGroupId);

            ArticleGroupHierarchyDto leaf = hierarchy.First(h => h.Level == 2);
            Assert.AreEqual("Gaming Laptops", leaf.Name);
            Assert.AreEqual("Electronics > Computers > Gaming Laptops", leaf.Path);
            Assert.AreEqual(454, leaf.ParentGroupId);
        }

        [TestMethod]
        public async Task GetHierarchyFromRootAsync_WithBranches_ReturnsAllDescendants()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            // Create branching hierarchy:
            //        Electronics (456)
            //       /            \
            //  Computers (457)   Audio (458)
            //     /       \
            // Laptops(459) Desktops(460)

            Result<ArticleGroup> root = ArticleGroup.Create(456, "Electronics");
            Result<ArticleGroup> computers = ArticleGroup.Create(457, "Computers", parentGroupId: 456);
            Result<ArticleGroup> audio = ArticleGroup.Create(458, "Audio", parentGroupId: 456);
            Result<ArticleGroup> laptops = ArticleGroup.Create(459, "Laptops", parentGroupId: 457);
            Result<ArticleGroup> desktops = ArticleGroup.Create(460, "Desktops", parentGroupId: 457);

            Assert.IsTrue(root.IsSuccess && computers.IsSuccess && audio.IsSuccess && laptops.IsSuccess && desktops.IsSuccess);

            _ = DbContext.ArticleGroups.Add(root.Value!);
            _ = DbContext.ArticleGroups.Add(computers.Value!);
            _ = DbContext.ArticleGroups.Add(audio.Value!);
            _ = DbContext.ArticleGroups.Add(laptops.Value!);
            _ = DbContext.ArticleGroups.Add(desktops.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetHierarchyFromRootAsync(new ArticleGroupId(456));

            Assert.AreEqual(5, hierarchy.Count);

            // Verify root
            Assert.AreEqual(1, hierarchy.Count(h => h.Level == 0));

            // Verify level 1 (Computers, Audio)
            Assert.AreEqual(2, hierarchy.Count(h => h.Level == 1));

            // Verify level 2 (Laptops, Desktops)
            Assert.AreEqual(2, hierarchy.Count(h => h.Level == 2));

            // Verify paths
            ArticleGroupHierarchyDto laptopsDto = hierarchy.First(h => h.Id == 459);
            Assert.AreEqual("Electronics > Computers > Laptops", laptopsDto.Path);

            ArticleGroupHierarchyDto audioDto = hierarchy.First(h => h.Id == 458);
            Assert.AreEqual("Electronics > Audio", audioDto.Path);
        }

        [TestMethod]
        public async Task GetHierarchyFromRootAsync_NonExistingRoot_ReturnsEmptyList()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetHierarchyFromRootAsync(new ArticleGroupId(9999));

            Assert.AreEqual(0, hierarchy.Count);
        }

        [TestMethod]
        public async Task GetHierarchyFromRootAsync_FromMiddleNode_ReturnsSubtree()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            // Create: Root (461) > Middle (462) > Leaf (463)
            Result<ArticleGroup> root = ArticleGroup.Create(461, "Root");
            Result<ArticleGroup> middle = ArticleGroup.Create(462, "Middle", parentGroupId: 461);
            Result<ArticleGroup> leaf = ArticleGroup.Create(463, "Leaf", parentGroupId: 462);

            Assert.IsTrue(root.IsSuccess && middle.IsSuccess && leaf.IsSuccess);

            _ = DbContext.ArticleGroups.Add(root.Value!);
            _ = DbContext.ArticleGroups.Add(middle.Value!);
            _ = DbContext.ArticleGroups.Add(leaf.Value!);
            _ = await DbContext.SaveChangesAsync();

            // Query from middle node
            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetHierarchyFromRootAsync(new ArticleGroupId(462));

            // Should only return Middle and Leaf (not Root)
            Assert.AreEqual(2, hierarchy.Count);

            ArticleGroupHierarchyDto middleDto = hierarchy.First(h => h.Level == 0);
            Assert.AreEqual(462, middleDto.Id);
            Assert.AreEqual("Middle", middleDto.Name);
            Assert.AreEqual("Middle", middleDto.Path);

            ArticleGroupHierarchyDto leafDto = hierarchy.First(h => h.Level == 1);
            Assert.AreEqual(463, leafDto.Id);
            Assert.AreEqual("Middle > Leaf", leafDto.Path);
        }

        [TestMethod]
        public async Task GetFullHierarchyAsync_EmptyDatabase_ReturnsEmptyList()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetFullHierarchyAsync();

            Assert.AreEqual(0, hierarchy.Count);
        }

        [TestMethod]
        public async Task GetFullHierarchyAsync_SingleRoot_ReturnsOne()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            Result<ArticleGroup> rootResult = ArticleGroup.Create(470, "Single Root");
            Assert.IsTrue(rootResult.IsSuccess);
            _ = DbContext.ArticleGroups.Add(rootResult.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetFullHierarchyAsync();

            Assert.AreEqual(1, hierarchy.Count);
            Assert.AreEqual(470, hierarchy[0].Id);
            Assert.AreEqual(0, hierarchy[0].Level);
        }

        [TestMethod]
        public async Task GetFullHierarchyAsync_MultipleRoots_ReturnsAllHierarchies()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            // Create two separate hierarchies
            // Hierarchy 1: Electronics (471) > Computers (472)
            // Hierarchy 2: Furniture (473) > Tables (474)

            Result<ArticleGroup> elec = ArticleGroup.Create(471, "Electronics");
            Result<ArticleGroup> comp = ArticleGroup.Create(472, "Computers", parentGroupId: 471);
            Result<ArticleGroup> furn = ArticleGroup.Create(473, "Furniture");
            Result<ArticleGroup> tables = ArticleGroup.Create(474, "Tables", parentGroupId: 473);

            Assert.IsTrue(elec.IsSuccess && comp.IsSuccess && furn.IsSuccess && tables.IsSuccess);

            _ = DbContext.ArticleGroups.Add(elec.Value!);
            _ = DbContext.ArticleGroups.Add(comp.Value!);
            _ = DbContext.ArticleGroups.Add(furn.Value!);
            _ = DbContext.ArticleGroups.Add(tables.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetFullHierarchyAsync();

            Assert.AreEqual(4, hierarchy.Count);

            // Verify both root nodes
            Assert.AreEqual(2, hierarchy.Count(h => h.Level == 0));
            Assert.AreEqual(2, hierarchy.Count(h => h.Level == 1));

            // Verify paths are separate
            Assert.IsTrue(hierarchy.Any(h => h.Path == "Electronics"));
            Assert.IsTrue(hierarchy.Any(h => h.Path == "Electronics > Computers"));
            Assert.IsTrue(hierarchy.Any(h => h.Path == "Furniture"));
            Assert.IsTrue(hierarchy.Any(h => h.Path == "Furniture > Tables"));
        }

        [TestMethod]
        public async Task GetFullHierarchyAsync_ComplexHierarchy_ReturnsOrderedByPath()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            // Create complex hierarchy with multiple branches
            //     Root1 (475)              Root2 (478)
            //     /      \                    |
            // Child1A  Child1B            Child2A
            //  (476)     (477)              (479)

            Result<ArticleGroup> root1 = ArticleGroup.Create(475, "AAA Root");
            Result<ArticleGroup> child1A = ArticleGroup.Create(476, "BBB Child", parentGroupId: 475);
            Result<ArticleGroup> child1B = ArticleGroup.Create(477, "AAA Child", parentGroupId: 475);
            Result<ArticleGroup> root2 = ArticleGroup.Create(478, "ZZZ Root");
            Result<ArticleGroup> child2A = ArticleGroup.Create(479, "Child", parentGroupId: 478);

            Assert.IsTrue(root1.IsSuccess && child1A.IsSuccess && child1B.IsSuccess && root2.IsSuccess && child2A.IsSuccess);

            _ = DbContext.ArticleGroups.Add(root1.Value!);
            _ = DbContext.ArticleGroups.Add(child1A.Value!);
            _ = DbContext.ArticleGroups.Add(child1B.Value!);
            _ = DbContext.ArticleGroups.Add(root2.Value!);
            _ = DbContext.ArticleGroups.Add(child2A.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetFullHierarchyAsync();

            Assert.AreEqual(5, hierarchy.Count);

            // Results should be ordered by Path (alphabetically)
            // Expected order: AAA Root, AAA Root > AAA Child, AAA Root > BBB Child, ZZZ Root, ZZZ Root > Child
            Assert.AreEqual("AAA Root", hierarchy[0].Path);
            Assert.AreEqual("AAA Root > AAA Child", hierarchy[1].Path);
            Assert.AreEqual("AAA Root > BBB Child", hierarchy[2].Path);
            Assert.AreEqual("ZZZ Root", hierarchy[3].Path);
            Assert.AreEqual("ZZZ Root > Child", hierarchy[4].Path);
        }

        [TestMethod]
        public async Task GetFullHierarchyAsync_DeepHierarchy_HandlesMultipleLevels()
        {
            Assert.IsNotNull(_repository);
            Assert.IsNotNull(DbContext);

            // Create 4-level deep hierarchy
            Result<ArticleGroup> level0 = ArticleGroup.Create(480, "Level0");
            Result<ArticleGroup> level1 = ArticleGroup.Create(481, "Level1", parentGroupId: 480);
            Result<ArticleGroup> level2 = ArticleGroup.Create(482, "Level2", parentGroupId: 481);
            Result<ArticleGroup> level3 = ArticleGroup.Create(483, "Level3", parentGroupId: 482);

            Assert.IsTrue(level0.IsSuccess && level1.IsSuccess && level2.IsSuccess && level3.IsSuccess);

            _ = DbContext.ArticleGroups.Add(level0.Value!);
            _ = DbContext.ArticleGroups.Add(level1.Value!);
            _ = DbContext.ArticleGroups.Add(level2.Value!);
            _ = DbContext.ArticleGroups.Add(level3.Value!);
            _ = await DbContext.SaveChangesAsync();

            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy = await _repository.GetFullHierarchyAsync();

            Assert.AreEqual(4, hierarchy.Count);

            // Verify levels are correct
            Assert.AreEqual(0, hierarchy.First(h => h.Id == 480).Level);
            Assert.AreEqual(1, hierarchy.First(h => h.Id == 481).Level);
            Assert.AreEqual(2, hierarchy.First(h => h.Id == 482).Level);
            Assert.AreEqual(3, hierarchy.First(h => h.Id == 483).Level);

            // Verify deepest path
            ArticleGroupHierarchyDto deepest = hierarchy.First(h => h.Id == 483);
            Assert.AreEqual("Level0 > Level1 > Level2 > Level3", deepest.Path);
        }
    }
}
