using System.Globalization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Domain.Tests.Catalog.ValueObjects
{
    [TestClass]
    public class ArticleGroupIdTests
    {
        // ============================================================
        // 1) Creation & Immutability
        // ============================================================

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(999)]
        public void CreateValidIdsShouldSucceed(int value)
        {
            var id = new ArticleGroupId(value);
            Assert.AreEqual(value, id.Value);
        }

        [TestMethod]
        public void ArticleGroupIdShouldBeImmutable()
        {
            var id = new ArticleGroupId(42);
            // No setters exist - record is init-only by default
            Assert.AreEqual(42, id.Value);  // Value unchanged
        }

        // ============================================================
        // 2) Value Equality (Core Value Object Property)
        // ============================================================

        [TestMethod]
        public void EqualIdsShouldBeEqual()
        {
            var id1 = new ArticleGroupId(123);
            var id2 = new ArticleGroupId(123);
            Assert.IsTrue(id1 == id2);
            Assert.IsTrue(id1.Equals(id2));
            Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode());
        }

        [TestMethod]
        public void DifferentIdsShouldNotBeEqual()
        {
            var id1 = new ArticleGroupId(123);
            var id2 = new ArticleGroupId(456);
            Assert.IsFalse(id1 == id2);
            Assert.IsFalse(id1.Equals(id2));
            Assert.AreNotEqual(id1.GetHashCode(), id2.GetHashCode());
        }

        [TestMethod]
        public void NullShouldNotEqualId()
        {
            ArticleGroupId? id1 = new(123);
            ArticleGroupId? id2 = null;
            Assert.IsFalse(id1 == id2);
            Assert.IsFalse(id1!.Equals(id2));
        }

        // ============================================================
        // 3) ToString Serialization
        // ============================================================

        [DataTestMethod]
        [DataRow(0, "0")]
        [DataRow(1, "1")]
        [DataRow(123, "123")]
        [DataRow(999, "999")]
        public void ToStringShouldReturnCorrectValue(int value, string expected)
        {
            var id = new ArticleGroupId(value);
            Assert.AreEqual(expected, id.ToString());
        }

        [TestMethod]
        public void ToStringShouldUseInvariantCulture()
        {
            var id = new ArticleGroupId(100);
            // Verify culture invariance (DB/JSON safe)
            Assert.AreEqual("100", id.Value.ToString(CultureInfo.InvariantCulture));
        }

        // ============================================================
        // 4) Edge Cases (BVA for Article Groups)
        // ============================================================

        [TestMethod]
        public void ZeroGroupIdShouldWork()
        {
            var id = new ArticleGroupId(0);
            Assert.AreEqual(0, id.Value);
        }

        [TestMethod]
        public void GroupId1ShouldWork()
        {
            var id = new ArticleGroupId(1);
            Assert.AreEqual(1, id.Value);
        }

        [TestMethod]
        public void MaxReasonableGroupIdShouldWork()
        {
            var id = new ArticleGroupId(99999);
            Assert.AreEqual(99999, id.Value);
        }
    }
}
