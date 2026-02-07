using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharedKernel.Primitives;

namespace SharedKernel.Tests.Primitives
{
    [TestClass]
    public class EmailTests
    {
        private static string Repeat(char c, int count) => new(c, count);

        // ============================================================
        // Create(...) — ECP
        // ============================================================

        [TestMethod]
        public void Create_ValidEmail_ShouldSucceed_AndLowercase()
        {
            Result<Email> r = Email.Create("  EDI.MUELLER@EXAMPLE.COM  ");
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual("edi.mueller@example.com", r.Value!.Value);
        }

        [TestMethod]
        public void Create_NullOrEmpty_ShouldFail()
        {
            Assert.IsFalse(Email.Create(null).IsSuccess);
            Assert.IsFalse(Email.Create("   ").IsSuccess);
        }

        [TestMethod]
        public void Create_InvalidFormat_ShouldFail()
        {
            Assert.IsFalse(Email.Create("not-an-email").IsSuccess);
            Assert.IsFalse(Email.Create("a@b").IsSuccess);     // no dot part
            Assert.IsFalse(Email.Create("a b@c.de").IsSuccess); // whitespace
        }

        // ============================================================
        // Create(...) — BVA (length)
        // ============================================================

        [TestMethod]
        public void Create_LengthBoundary255_ShouldSucceed()
        {
            // Construct a valid email with total length exactly 255.
            // Example: local(=243 chars) + "@a.de"(=5) + maybe adjust -> total 255
            // Let's compute: local + "@example.com" etc. We'll do deterministic approach:
            string domain = "@example.com"; // 12
            int localLen = 255 - domain.Length;
            string local = Repeat('a', localLen);
            string email = local + domain;

            Result<Email> r = Email.Create(email);
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(email.ToLowerInvariant(), r.Value!.Value);
        }

        [TestMethod]
        public void Create_LengthBoundary256_ShouldFail()
        {
            string domain = "@example.com"; // 12
            int localLen = 256 - domain.Length;
            string local = Repeat('a', localLen);
            string email = local + domain; // length 256

            Result<Email> r = Email.Create(email);
            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // Equality
        // ============================================================

        [TestMethod]
        public void Equality_SameNormalizedValue_ShouldBeEqual()
        {
            Email a = Email.Create("EDI@EXAMPLE.COM").Value!;
            Email b = Email.Create("edi@example.com").Value!;
            Assert.AreEqual(a, b);
        }

        [TestMethod]
        public void Equality_DifferentValues_ShouldNotBeEqual()
        {
            Email a = Email.Create("a@example.com").Value!;
            Email b = Email.Create("b@example.com").Value!;
            Assert.AreNotEqual(a, b);
        }
    }
}
