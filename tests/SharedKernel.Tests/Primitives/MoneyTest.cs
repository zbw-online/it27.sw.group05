using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharedKernel.Primitives;

namespace SharedKernel.Tests.Primitives
{
    [TestClass]
    public class MoneyTests
    {
        // ============================================================
        // From(...) — ECP
        // ============================================================

        [TestMethod]
        public void From_ValidInputs_ShouldSucceed_AndNormalize()
        {
            Result<Money> r = Money.From(12.345m, " chf ");
            Assert.IsTrue(r.IsSuccess);

            Assert.AreEqual(12.35m, r.Value!.Amount); // AwayFromZero
            Assert.AreEqual("CHF", r.Value!.Currency);
            Assert.AreEqual("12.35 CHF", r.Value!.ToString());
        }

        [TestMethod]
        public void From_NegativeAmount_ShouldFail() => Assert.IsFalse(Money.From(-0.01m, "CHF").IsSuccess);

        [TestMethod]
        public void From_InvalidCurrency_ShouldFail()
        {
            Assert.IsFalse(Money.From(1m, "").IsSuccess);
            Assert.IsFalse(Money.From(1m, "  ").IsSuccess);
            Assert.IsFalse(Money.From(1m, "CH").IsSuccess);
            Assert.IsFalse(Money.From(1m, "CHFF").IsSuccess);
        }

        // ============================================================
        // From(...) — BVA
        // ============================================================

        [TestMethod]
        public void From_Boundary_ZeroAmount_ShouldSucceed()
        {
            Result<Money> r = Money.From(0m, "CHF");
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(0m, r.Value!.Amount);
        }

        [TestMethod]
        public void From_Boundary_CurrencyLength3_ShouldSucceed()
        {
            Result<Money> r = Money.From(1m, "EUR");
            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual("EUR", r.Value!.Currency);
        }

        // ============================================================
        // Operators — ECP
        // ============================================================

        [TestMethod]
        public void Add_SameCurrency_ShouldSucceed()
        {
            Money a = Money.From(10m, "CHF").Value!;
            Money b = Money.From(2.5m, "CHF").Value!;

            Money sum = a + b;
            Assert.AreEqual(12.5m, sum.Amount);
            Assert.AreEqual("CHF", sum.Currency);
        }

        [TestMethod]
        public void Subtract_SameCurrency_ShouldSucceed()
        {
            Money a = Money.From(10m, "CHF").Value!;
            Money b = Money.From(2.5m, "CHF").Value!;

            Money diff = a - b;
            Assert.AreEqual(7.5m, diff.Amount);
            Assert.AreEqual("CHF", diff.Currency);
        }

        [TestMethod]
        public void Add_CurrencyMismatch_ShouldThrowOrFailFast()
        {
            // Your Money uses EnsureSameCurrency(...).EnsureSuccess() -> likely throws.
            Money a = Money.From(10m, "CHF").Value!;
            Money b = Money.From(2m, "EUR").Value!;

            _ = Assert.ThrowsException<DomainException>(() =>
            {
                _ = a + b;
            });
        }

        // ============================================================
        // Multiply / Divide — BVA
        // ============================================================

        [TestMethod]
        public void Multiply_ByZero_ShouldReturnZero()
        {
            Money a = Money.From(10m, "CHF").Value!;
            Money res = a * 0;
            Assert.AreEqual(0m, res.Amount);
            Assert.AreEqual("CHF", res.Currency);
        }

        [TestMethod]
        public void Divide_ByTwo_ShouldRound()
        {
            Money a = Money.From(10m, "CHF").Value!;
            Money res = a / 3; // 3.333.. -> 3.33 AwayFromZero? actually 3.33
            Assert.AreEqual("CHF", res.Currency);
            Assert.AreEqual(3.33m, res.Amount);
        }

        [TestMethod]
        public void Divide_ByZero_ShouldThrowOrFailFast()
        {
            Money a = Money.From(10m, "CHF").Value!;
            _ = Assert.ThrowsException<DomainException>(() =>
            {
                _ = a / 0;
            });
        }

        // ============================================================
        // Equality
        // ============================================================

        [TestMethod]
        public void Equality_SameAmountAndCurrency_ShouldBeEqual()
        {
            Money a = Money.From(10m, "CHF").Value!;
            Money b = Money.From(10.000m, "chf").Value!;

            Assert.AreEqual(a, b);
        }

        [TestMethod]
        public void Equality_DifferentCurrency_ShouldNotBeEqual()
        {
            Money a = Money.From(10m, "CHF").Value!;
            Money b = Money.From(10m, "EUR").Value!;
            Assert.AreNotEqual(a, b);
        }
    }
}
