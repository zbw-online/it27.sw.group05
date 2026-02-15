using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers;

namespace OrderManagement.Domain.Tests.Customers
{
    [TestClass]
    public class CustomerAddressTests
    {
        private static CustomerAddress CreateAddress(
            DateOnly validFrom,
            DateOnly? validTo)
        {
            // internal ctor: (int id, DateOnly validFrom, DateOnly? validTo, string street, string houseNumber, string postalCode, string city, string countryCode)
            ConstructorInfo ctor = typeof(CustomerAddress)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(c => c.GetParameters().Length == 8);

            return (CustomerAddress)ctor.Invoke(
            [
                1,
                validFrom,
                validTo,
                "Seestrasse",
                "55a",
                "8002",
                "Zuerich",
                "CH"
            ]);
        }

        private static void Close(CustomerAddress address, DateOnly validTo)
        {
            // internal method Close(DateOnly)
            MethodInfo close = typeof(CustomerAddress)
                .GetMethod("Close", BindingFlags.Instance | BindingFlags.NonPublic)!;

            _ = close.Invoke(address, [validTo]);
        }

        // ============================================================
        // IsAciveOn(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void IsActiveOn_OpenEndedRange_DateInside_ShouldReturnTrue()
        {
            // ECP: Valid class (ValidTo is null; date >= ValidFrom)
            CustomerAddress addr = CreateAddress(validFrom: new DateOnly(2025, 01, 01), validTo: null);

            Assert.IsTrue(addr.IsActiveOn(new DateOnly(2025, 01, 01)));
            Assert.IsTrue(addr.IsActiveOn(new DateOnly(2025, 12, 31)));
        }

        [TestMethod]
        public void IsActiveOn_OpenEndedRange_DateBeforeValidFrom_ShouldReturnFalse()
        {
            // ECP: Invalid class (date < ValidFrom)
            CustomerAddress addr = CreateAddress(validFrom: new DateOnly(2025, 01, 10), validTo: null);

            Assert.IsFalse(addr.IsActiveOn(new DateOnly(2025, 01, 09)));
        }

        [TestMethod]
        public void IsActiveOn_ClosedRange_DateAfterValidTo_ShouldReturnFalse()
        {
            // ECP: Invalid class (date > ValidTo)
            CustomerAddress addr = CreateAddress(
                validFrom: new DateOnly(2025, 01, 01),
                validTo: new DateOnly(2025, 01, 31));

            Assert.IsFalse(addr.IsActiveOn(new DateOnly(2025, 02, 01)));
        }

        // ============================================================
        // IsAciveOn(...) — Boundary Value Analysis
        // ============================================================

        [TestMethod]
        public void IsActiveOn_Boundary_OnValidFrom_ShouldReturnTrue()
        {
            CustomerAddress addr = CreateAddress(
                validFrom: new DateOnly(2025, 01, 01),
                validTo: new DateOnly(2025, 01, 31));

            // BVA: date == ValidFrom
            Assert.IsTrue(addr.IsActiveOn(new DateOnly(2025, 01, 01)));
        }

        [TestMethod]
        public void IsActiveOn_Boundary_OnValidTo_ShouldReturnTrue()
        {
            CustomerAddress addr = CreateAddress(
                validFrom: new DateOnly(2025, 01, 01),
                validTo: new DateOnly(2025, 01, 31));

            // BVA: date == ValidTo
            Assert.IsTrue(addr.IsActiveOn(new DateOnly(2025, 01, 31)));
        }

        [TestMethod]
        public void IsActiveOn_Boundary_DayBeforeValidFrom_ShouldReturnFalse()
        {
            CustomerAddress addr = CreateAddress(
                validFrom: new DateOnly(2025, 01, 01),
                validTo: new DateOnly(2025, 01, 31));

            // BVA: date == ValidFrom - 1
            Assert.IsFalse(addr.IsActiveOn(new DateOnly(2024, 12, 31)));
        }

        [TestMethod]
        public void IsActiveOn_Boundary_DayAfterValidTo_ShouldReturnFalse()
        {
            CustomerAddress addr = CreateAddress(
                validFrom: new DateOnly(2025, 01, 01),
                validTo: new DateOnly(2025, 01, 31));

            // BVA: date == ValidTo + 1
            Assert.IsFalse(addr.IsActiveOn(new DateOnly(2025, 02, 01)));
        }

        // ============================================================
        // Close(...) — Equivalence + Boundary
        // ============================================================

        [TestMethod]
        public void Close_ShouldSetValidTo()
        {
            // ECP: valid close
            CustomerAddress addr = CreateAddress(validFrom: new DateOnly(2025, 01, 01), validTo: null);
            Assert.IsNull(addr.ValidTo);

            Close(addr, new DateOnly(2025, 01, 31));

            Assert.AreEqual(new DateOnly(2025, 01, 31), addr.ValidTo);
        }

        [TestMethod]
        public void PrivateParameterlessConstructor_ShouldBeCoverable()
        {
            // Coverage: private ctor
            var addr = (CustomerAddress)Activator.CreateInstance(typeof(CustomerAddress), nonPublic: true)!;
            Assert.IsNotNull(addr);
        }
    }
}
