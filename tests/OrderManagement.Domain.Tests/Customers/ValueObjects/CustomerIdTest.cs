using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers.ValueObjects;

namespace OrderManagement.Domain.Tests.Customers.ValueObjects
{
    [TestClass]
    public class CustomerIdTests
    {
        [TestMethod]
        public void ToString_ShouldUseInvariantCulture()
        {
            var id = new CustomerId(12345);
            Assert.AreEqual("12345", id.ToString());
        }

        [TestMethod]
        public void Equality_SameValue_ShouldBeEqual()
        {
            var a = new CustomerId(1);
            var b = new CustomerId(1);
            Assert.AreEqual(a, b);
        }

        [TestMethod]
        public void Boundary_ZeroAndNegative_ShouldBeRepresentable()
        {
            // BVA: 0 and negative exist at type level (domain may prohibit elsewhere)
            Assert.AreEqual("0", new CustomerId(0).ToString());
            Assert.AreEqual("-1", new CustomerId(-1).ToString());
        }
    }
}
