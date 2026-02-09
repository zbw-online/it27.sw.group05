using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.Events;

using SharedKernel.Primitives;

namespace OrderManagement.Domain.Tests.Catalog
{
    [TestClass]
    public class ArticleGroupEquivalenceAndBoundaryTests
    {
        // -----------------------------
        // Helpers
        // -----------------------------
        private static Result<ArticleGroup> CreateValidGroup(
            int id = 1,
            string name = "Electronics",
            int? parentGroupId = null)
            => ArticleGroup.Create(id, name, parentGroupId);

        private static string Repeat(char c, int count) => new(c, count);

        // ============================================================
        // 1) Create(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void CreateValidInputsShouldSucceedAndRaiseCreatedEvent()
        {
            // ECP: Valid article group
            Result<ArticleGroup> r = CreateValidGroup();

            Assert.IsTrue(r.IsSuccess);
            ArticleGroup g = r.Value!;
            Assert.AreEqual("Electronics", g.Name);
            Assert.IsTrue(g.DomainEvents.Count >= 1);   // ArticleGroupCreated
        }

        [TestMethod]
        public void CreateInvalidIdNegativeShouldFail()
        {
            // ECP: Invalid id class (id < 0)
            Result<ArticleGroup> r = CreateValidGroup(id: -1);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateNameWhitespaceOnlyShouldFail()
        {
            // ECP: Invalid name class (empty after trim)
            Result<ArticleGroup> r = CreateValidGroup(name: "   ");

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateInvalidParentGroupIdNegativeShouldFail()
        {
            // ECP: Invalid parent group id (parentGroupId <= 0)
            Result<ArticleGroup> r = CreateValidGroup(parentGroupId: -5);

            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // 2) Create(...) — Boundary Value Analysis (Name length)
        //    ERM: Name nvarchar(150) → max 150 chars
        // ============================================================

        [TestMethod]
        public void CreateNameLengthBoundary150ShouldSucceed()
        {
            // BVA: name length = 150 (max valid)
            string name = Repeat('A', 150);

            Result<ArticleGroup> r = CreateValidGroup(name: name);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(name, r.Value!.Name);
        }

        [TestMethod]
        public void CreateNameLengthBoundary151ShouldFail()
        {
            // BVA: name length = 151 (just over max) – you will enforce this in value checks if desired
            string name = Repeat('A', 151);

            Result<ArticleGroup> r = CreateValidGroup(name: name);

            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // 3) Rename(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void RenameValidNameShouldSucceedAndRaiseRenamedEvent()
        {
            Result<ArticleGroup> r = CreateValidGroup();
            ArticleGroup g = r.Value!;

            Result rename = g.Rename("Home Electronics");

            Assert.IsTrue(rename.IsSuccess);
            Assert.AreEqual("Home Electronics", g.Name);
            Assert.IsTrue(g.DomainEvents.Any(e => e is ArticleGroupRenamed));
        }

        [TestMethod]
        public void RenameNameWhitespaceOnlyShouldFail()
        {
            Result<ArticleGroup> r = CreateValidGroup();
            ArticleGroup g = r.Value!;

            Result rename = g.Rename("   ");

            Assert.IsFalse(rename.IsSuccess);
            Assert.AreEqual("Electronics", g.Name);
        }

        // ============================================================
        // 4) AddChild(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void AddChildValidChildShouldSucceed()
        {
            Result<ArticleGroup> rootResult = CreateValidGroup(id: 1, name: "Root");
            Result<ArticleGroup> childResult = CreateValidGroup(id: 2, name: "Child", parentGroupId: 1);

            ArticleGroup root = rootResult.Value!;
            ArticleGroup child = childResult.Value!;

            Result addChildResult = root.AddChild(child);

            Assert.IsTrue(addChildResult.IsSuccess);
            Assert.AreEqual(1, root.Children.Count);
            Assert.AreSame(child, root.Children.Single());
        }

        [TestMethod]
        public void AddChildDuplicateChildShouldFail()
        {
            Result<ArticleGroup> rootResult = CreateValidGroup(id: 1, name: "Root");
            Result<ArticleGroup> childResult = CreateValidGroup(id: 2, name: "Child", parentGroupId: 1);

            ArticleGroup root = rootResult.Value!;
            ArticleGroup child = childResult.Value!;

            Result firstAdd = root.AddChild(child);
            Assert.IsTrue(firstAdd.IsSuccess);

            Result secondAdd = root.AddChild(child);

            Assert.IsFalse(secondAdd.IsSuccess);
        }

        // ============================================================
        // 5) HasCircularReference(...) — Basic ECP
        // ============================================================

        [TestMethod]
        public void AddChildWhenChildWouldCreateCircularReferenceShouldFail()
        {
            // Arrange: root(1) has ParentGroupId=2 (child), creating cycle potential
            Result<ArticleGroup> childResult = CreateValidGroup(id: 2, name: "Child");
            Result<ArticleGroup> rootResult = CreateValidGroup(id: 1, name: "Root", parentGroupId: 2);

            Assert.IsTrue(childResult.IsSuccess);
            Assert.IsTrue(rootResult.IsSuccess);

            ArticleGroup root = rootResult.Value!;
            ArticleGroup child = childResult.Value!;

            // Act: root.AddChild(child) → detects ParentGroupId=child.Id in traversal
            Result addResult = root.AddChild(child);

            // Assert: Fails due to circular reference detection
            Assert.IsFalse(addResult.IsSuccess);
            // Optional: StringAssert.Contains(addResult.Error, "circular"); // if exact match
        }


    }
}
