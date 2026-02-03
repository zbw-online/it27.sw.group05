using OrderManagement.Domain.Catalog.ValueObjects;

using System.Globalization;

namespace OrderManagement.Domain.Tests.Catalog.ValueObjects
{
    [TestClass]
    public class ArticleIdTests
    {
        // ============================================================
        // 1) Creation & Immutability
        // ============================================================

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(1000)]
        [DataRow(int.MaxValue)]
        public void CreateValidIdsShouldSucceed(int value)
        {
            var id = new ArticleId(value);
            Assert.AreEqual(value, id.Value);
        }

        [TestMethod]
        public void ArticleIdShouldBeImmutable()
        {
            var id = new ArticleId(42);
            // No setters exist - record is init-only by default
            Assert.AreEqual(42, id.Value);  // Value unchanged
        }

        // ============================================================
        // 2) Value Equality (Core Value Object Property)
        // ============================================================

        [TestMethod]
        public void EqualIdsShouldBeEqual()
        {
            var id1 = new ArticleId(123);
            var id2 = new ArticleId(123);
            Assert.IsTrue(id1 == id2);
            Assert.IsTrue(id1.Equals(id2));
            Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode());
        }

        [TestMethod]
        public void DifferentIdsShouldNotBeEqual()
        {
            var id1 = new ArticleId(123);
            var id2 = new ArticleId(456);
            Assert.IsFalse(id1 == id2);
            Assert.IsFalse(id1.Equals(id2));
            Assert.AreNotEqual(id1.GetHashCode(), id2.GetHashCode());
        }

        [TestMethod]
        public void NullShouldNotEqualId()
        {
            ArticleId? id1 = new(123);
            ArticleId? id2 = null;
            Assert.IsFalse(id1 == id2);
            Assert.IsFalse(id1!.Equals(id2));
        }

        // ============================================================
        // 3) ToString Serialization
        // ============================================================

        [DataTestMethod]
        [DataRow(0, "0")]
        [DataRow(123, "123")]
        [DataRow(-456, "-456")]
        [DataRow(int.MaxValue, "2147483647")]
        public void ToStringShouldReturnCorrectValue(int value, string expected)
        {
            var id = new ArticleId(value);
            Assert.AreEqual(expected, id.ToString());
        }

        [TestMethod]
        public void ToStringShouldUseInvariantCulture()
        {
            // Tests culture-invariance (important for DB/JSON serialization)
            var id = new ArticleId(1000);
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");  // Uses comma for thousands
                Assert.AreEqual("1000", id.ToString());  // Ignores current culture
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        // ============================================================
        // 4) Edge Cases (BVA)
        // ============================================================

        [TestMethod]
        public void ZeroIdShouldWork()
        {
            var id = new ArticleId(0);
            Assert.AreEqual(0, id.Value);
        }

        [TestMethod]
        public void NegativeIdShouldWork()
        {
            var id = new ArticleId(-1);
            Assert.AreEqual(-1, id.Value);
        }

        [TestMethod]
        public void MaxIntIdShouldWork()
        {
            var id = new ArticleId(int.MaxValue);
            Assert.AreEqual(int.MaxValue, id.Value);
        }
    }
}
