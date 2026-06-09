using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers.ValueObjects;

namespace OrderManagement.Domain.Tests.Customers.ValueObjects
{
    [TestClass]
    public class CustomerNumberTests
    {
        // ============================================================
        // Create(...) — Equivalence Partitioning
        // ============================================================

        [TestMethod]
        public void Create_ValidFormat_ShouldSucceed_AndNormalize()
        {
            SharedKernel.Primitives.Result<CustomerNumber> r = CustomerNumber.Create(" cu00001 ");
            Assert.IsTrue(r.IsSuccess);

            Assert.AreEqual("CU00001", r.Value!.Value);
            Assert.AreEqual("CU00001", r.Value!.ToString());
        }

        [TestMethod]
        public void Create_NullOrWhitespace_ShouldFail()
        {
            Assert.IsFalse(CustomerNumber.Create(null).IsSuccess);
            Assert.IsFalse(CustomerNumber.Create("   ").IsSuccess);
        }

        [TestMethod]
        public void Create_InvalidFormat_ShouldFail()
        {
            Assert.IsFalse(CustomerNumber.Create("C-1").IsSuccess);
            Assert.IsFalse(CustomerNumber.Create("XU00001").IsSuccess);
            Assert.IsFalse(CustomerNumber.Create("C-ABCDE").IsSuccess);
        }

        // ============================================================
        // Create(...) — Boundary Value Analysis (Regex exactness)
        // ============================================================

        [TestMethod]
        public void Create_Boundary_ExactFiveDigits_ShouldSucceed()
        {
            SharedKernel.Primitives.Result<CustomerNumber> r = CustomerNumber.Create("CU00000");
            Assert.IsTrue(r.IsSuccess);

            SharedKernel.Primitives.Result<CustomerNumber> r2 = CustomerNumber.Create("CU99999");
            Assert.IsTrue(r2.IsSuccess);
        }

        [TestMethod]
        public void Create_Boundary_FourDigits_ShouldFail() =>
            // BVA: one digit short
            Assert.IsFalse(CustomerNumber.Create("CU0000").IsSuccess);

        [TestMethod]
        public void Create_Boundary_SixDigits_ShouldFail() =>
            // BVA: one digit too many
            Assert.IsFalse(CustomerNumber.Create("CU000001").IsSuccess);

        // ============================================================
        // Equality (ValueObject semantics)
        // ============================================================

        [TestMethod]
        public void Equality_SameNormalizedValue_ShouldBeEqual()
        {
            CustomerNumber a = CustomerNumber.Create("cU00001").Value!;
            CustomerNumber b = CustomerNumber.Create(" CU00001 ").Value!;

            Assert.AreEqual(a, b);
            Assert.IsTrue(a.Equals(b));
        }

        [TestMethod]
        public void Equality_DifferentValues_ShouldNotBeEqual()
        {
            CustomerNumber a = CustomerNumber.Create("CU00001").Value!;
            CustomerNumber b = CustomerNumber.Create("CU00002").Value!;

            Assert.AreNotEqual(a, b);
        }
    }
}
