using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Tests.Catalog.ValueObjects
{
    [TestClass]
    public class ArticleNumberTests
    {
        // ============================================================
        // 1) Create Factory — Equivalence Classes (ECP)
        // ============================================================

        [DataTestMethod]
        [DataRow(null, false, "Article number is required.")]
        [DataRow("", false, "Article number is required.")]
        [DataRow("   ", false, "Article number is required.")]
        [DataRow("ART001", true)]
        [DataRow("ABC123", true)]
        [DataRow("A-B-C", true)]
        [DataRow("123-456", true)]
        [DataRow("ABC123-DEF456", true)]
        [DataRow("aBc123", true)]
        [DataRow("ABC!123", false, "Article number has an invalid format.")]
        [DataRow("ABC 123", false, "Article number has an invalid format.")]
        public void CreateEquivalenceClasses(
            string? input, bool expectedSuccess, string? expectedError = null)
        {
            Result<ArticleNumber> result = ArticleNumber.Create(input);

            Assert.AreEqual(expectedSuccess, result.IsSuccess, $"Input: '{input}'");

            if (expectedSuccess)
            {
                Assert.AreEqual(input!.Trim().ToUpperInvariant(), result.Value!.Value);
            }
            else
            {
                Assert.AreEqual(expectedError, result.Error);
            }
        }

        // ============================================================
        // 2) Create Factory — Boundary Value Analysis (BVA)
        // ============================================================

        [TestMethod]
        public void CreateMinLength1ShouldSucceed()
        {
            Result<ArticleNumber> result = ArticleNumber.Create("A");
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("A", result.Value!.Value);
        }

        [TestMethod]
        public void CreateMaxLength20ShouldSucceed()
        {
            string maxValid = new('A', 20);
            Result<ArticleNumber> result = ArticleNumber.Create(maxValid);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(maxValid, result.Value!.Value);
        }

        [TestMethod]
        public void CreateLength21ShouldFail()
        {
            string tooLong = new('A', 21);
            Result<ArticleNumber> result = ArticleNumber.Create(tooLong);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Article number must be at most 20 characters.", result.Error);
        }

        [TestMethod]
        public void CreateShouldTrimWhitespace()
        {
            Result<ArticleNumber> result = ArticleNumber.Create("  ART001  ");
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("ART001", result.Value!.Value);
        }

        [TestMethod]
        public void CreateShouldUppercaseInput()
        {
            Result<ArticleNumber> result = ArticleNumber.Create("art001");
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("ART001", result.Value!.Value);
        }

        // ============================================================
        // 3) Value Equality
        // ============================================================

        [TestMethod]
        public void EqualArticleNumbersShouldBeEqual()
        {
            ArticleNumber num1 = ArticleNumber.Create("ART001")!.Value!;
            ArticleNumber num2 = ArticleNumber.Create("art001")!.Value!;
            ArticleNumber num3 = ArticleNumber.Create("  Art001  ")!.Value!;

            Assert.IsTrue(num1 == num2);
            Assert.IsTrue(num1 == num3);
            Assert.IsTrue(num1.Equals(num2));
            Assert.AreEqual(num1.GetHashCode(), num2.GetHashCode());
        }

        [TestMethod]
        public void DifferentArticleNumbersShouldNotBeEqual()
        {
            ArticleNumber num1 = ArticleNumber.Create("ART001")!.Value!;
            ArticleNumber num2 = ArticleNumber.Create("ART002")!.Value!;
            Assert.IsFalse(num1 == num2);
            Assert.IsFalse(num1.Equals(num2));
            Assert.AreNotEqual(num1.GetHashCode(), num2.GetHashCode());
        }

        [TestMethod]
        public void NullShouldNotEqualArticleNumber()
        {
            ArticleNumber num = ArticleNumber.Create("ART001")!.Value!;
            Assert.IsFalse(num == null);
        }

        // ============================================================
        // 4) Immutability
        // ============================================================

        [TestMethod]
        public void ArticleNumberShouldBeImmutable()
        {
            ArticleNumber num = ArticleNumber.Create("TEST")!.Value!;
            // No public setters - private constructor only
            Assert.AreEqual("TEST", num.Value);
        }

        // ============================================================
        // 5) ToString
        // ============================================================

        [TestMethod]
        public void ToStringShouldReturnNormalizedValue()
        {
            ArticleNumber num = ArticleNumber.Create("abc-123")!.Value!;
            Assert.AreEqual("ABC-123", num.ToString());
        }

        // ============================================================
        // 6) ValueObject Base Class — Equality Components
        // ============================================================

        [TestMethod]
        public void GetEqualityComponentsShouldReturnSingleValue()
        {
            ArticleNumber num = ArticleNumber.Create("ABC123")!.Value!;
            // Use reflection to access the protected method for testing purposes
            System.Reflection.MethodInfo? method = typeof(ValueObject).GetMethod("GetEqualityComponents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var components = ((IEnumerable<object?>)method!.Invoke(num, null)!).ToList();

            Assert.AreEqual(1, components.Count);
            Assert.AreEqual("ABC123", components[0]);
        }
    }
}
