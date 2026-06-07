using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.Events;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Domain.Tests.Catalog
{
    [TestClass]
    public sealed class ArticleGroupTests
    {
        private static Result<ArticleGroup> CreateValidGroup(
            string name = "Electronics",
            ArticleGroupId? parentGroupId = null) => ArticleGroup.Create(name, parentGroupId);

        [TestMethod]
        public void Create_WithValidName_ShouldSucceed()
        {
            Result<ArticleGroup> result = CreateValidGroup();

            Assert.IsTrue(result.IsSuccess, result.Error);

            ArticleGroup group = result.Value!;

            Assert.AreEqual(0, group.Id.Value);
            Assert.AreEqual("Electronics", group.Name);
            Assert.IsNull(group.ParentGroupId);
        }

        [TestMethod]
        public void Create_WithValidName_ShouldRaiseCreatedEvent()
        {
            ArticleGroup group = CreateValidGroup().EnsureValue();

            Assert.IsTrue(group.DomainEvents.Any(e => e is ArticleGroupCreated));
        }

        [TestMethod]
        public void Create_WithWhitespaceName_ShouldFail()
        {
            Result<ArticleGroup> result = CreateValidGroup("   ");

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Create_WithNameLongerThan150Characters_ShouldFail()
        {
            string name = new('A', 151);

            Result<ArticleGroup> result = CreateValidGroup(name);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Create_WithAssignedParentGroupId_ShouldSucceed()
        {
            var parentGroupId = new ArticleGroupId(10);

            Result<ArticleGroup> result = CreateValidGroup(
                name: "Child Group",
                parentGroupId: parentGroupId);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(parentGroupId, result.Value!.ParentGroupId);
        }

        [TestMethod]
        public void Create_WithUnassignedParentGroupId_ShouldFail()
        {
            Result<ArticleGroup> result = CreateValidGroup(
                name: "Child Group",
                parentGroupId: ArticleGroupId.Empty);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Rename_WithValidName_ShouldSucceed()
        {
            ArticleGroup group = CreateValidGroup().EnsureValue();

            Result result = group.Rename("Updated Name");

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Updated Name", group.Name);
        }

        [TestMethod]
        public void Rename_WithValidName_ShouldRaiseRenamedEvent()
        {
            ArticleGroup group = CreateValidGroup().EnsureValue();

            Result result = group.Rename("Updated Name");

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(group.DomainEvents.Any(e => e is ArticleGroupRenamed));
        }

        [TestMethod]
        public void Rename_WithWhitespaceName_ShouldFail()
        {
            ArticleGroup group = CreateValidGroup().EnsureValue();

            Result result = group.Rename("   ");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Electronics", group.Name);
        }

        [TestMethod]
        public void Rename_WithNameLongerThan150Characters_ShouldFail()
        {
            ArticleGroup group = CreateValidGroup().EnsureValue();
            string name = new('A', 151);

            Result result = group.Rename(name);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Electronics", group.Name);
        }
    }
}
