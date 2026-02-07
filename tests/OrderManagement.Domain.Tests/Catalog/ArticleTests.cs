using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.Events;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Domain.Tests.Catalog
{
    [TestClass]
    public class ArticleEquivalenceAndBoundaryTests
    {
        // -----------------------------
        // Helpers
        // -----------------------------
        private static Result<Article> CreateValidArticle(
            int id = 1,
            string? articleNr = "ART001",
            string name = "Test Product",
            decimal priceAmount = 99.99m,
            string priceCurrency = "CHF",
            int groupId = 1,
            int stock = 10,
            decimal vatRate = 7.7m,
            string? description = "Test description")
            => Article.Create(id, articleNr, name, priceAmount, priceCurrency, groupId, stock, vatRate, description);

        private static string Repeat(char c, int count) => new(c, count);

        // ============================================================
        // 1) Create(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void CreateValidInputsShouldSucceedAndRaiseCreatedEvent()
        {
            // ECP: Valid article with all properties
            Result<Article> r = CreateValidArticle();

            Assert.IsTrue(r.IsSuccess);
            Article a = r.Value!;
            Assert.AreEqual("ART001", a.ArticleNumber.Value);
            Assert.AreEqual("Test Product", a.Name);
            Assert.AreEqual(99.99m, a.Price.Amount);
            Assert.AreEqual("CHF", a.Price.Currency);
            Assert.AreEqual(1, a.ArticleGroupId.Value);
            Assert.IsTrue(a.DomainEvents.Count >= 1);   // ArticleCreated
        }

        [TestMethod]
        public void CreateInvalidIdNegativeShouldFail()
        {
            // ECP: Invalid id class (id <= 0)
            Result<Article> r = CreateValidArticle(id: -1);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateNameWhitespaceOnlyShouldFail()
        {
            // ECP: Invalid name class (empty after trim)
            Result<Article> r = CreateValidArticle(name: "   ");

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateInvalidGroupIdNegativeShouldFail()
        {
            // ECP: Invalid group id (groupId <= 0)
            Result<Article> r = CreateValidArticle(groupId: -1);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateInvalidStockNegativeShouldFail()
        {
            // ECP: Invalid stock (stock < 0)
            Result<Article> r = CreateValidArticle(stock: -1);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateArticleNumberInvalidFromValueObjectShouldFail()
        {
            // ECP: Invalid ArticleNumber (handled by ArticleNumber.Create)
            Result<Article> r = Article.Create(1, new string('A', 21), "Valid Name", 10.0m, "CHF", 1);

            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // 2) Create(...) — Boundary Value Analysis
        // ============================================================

        [TestMethod]
        public void CreateArticleNumberLengthBoundary20ShouldSucceed()
        {
            // BVA: ArticleNumber length = 20 (max valid)
            string nr = Repeat('A', 20);
            Result<Article> r = Article.Create(1, nr, "Valid Name", 10.0m, "CHF", 1);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(nr, r.Value!.ArticleNumber.Value);
        }

        [TestMethod]
        public void CreateArticleNumberLengthBoundary21ShouldFail()
        {
            // BVA: ArticleNumber length = 21 (over max)
            string nr = Repeat('A', 21);
            Result<Article> r = Article.Create(1, nr, "Valid Name", 10.0m, "CHF", 1);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateNameLengthBoundary200ShouldSucceed()
        {
            // BVA: name length = 200 (max valid)
            string name = Repeat('A', 200);
            Result<Article> r = CreateValidArticle(name: name);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(name, r.Value!.Name);
        }

        [TestMethod]
        public void CreateNameLengthBoundary201ShouldFail()
        {
            // BVA: name length = 201 (over max)
            string name = Repeat('A', 201);
            Result<Article> r = CreateValidArticle(name: name);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateVatRateBoundaryMin0ShouldSucceed()
        {
            // BVA: VatRate = 0.00 (min valid, decimal(5,2))
            Result<Article> r = CreateValidArticle(vatRate: 0.00m);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(0.00m, r.Value!.VatRate);
        }

        [TestMethod]
        public void CreateVatRateBoundaryMax99999ShouldSucceed()
        {
            // BVA: VatRate = 999.99 (max valid)
            Result<Article> r = CreateValidArticle(vatRate: 999.99m);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(999.99m, r.Value!.VatRate);
        }

        [TestMethod]
        public void CreateVatRateBoundaryOverMax1000ShouldFail()
        {
            // BVA: VatRate > 999.99
            Result<Article> r = CreateValidArticle(vatRate: 1000.00m);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateVatRateNegativeShouldFail()
        {
            // BVA: VatRate < 0
            Result<Article> r = CreateValidArticle(vatRate: -0.01m);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateVatRateMoreThan2DecimalsShouldFail()
        {
            // BVA: VatRate with >2 decimals (e.g. 7.777 → fails floor check)
            Result<Article> r = CreateValidArticle(vatRate: 7.777m);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateVatRateValid2DecimalsShouldSucceed()
        {
            // BVA: Valid VatRate 7.70 (2 decimals, Swiss standard rate [web:8])
            Result<Article> r = CreateValidArticle(vatRate: 7.70m);

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(7.70m, r.Value!.VatRate);
        }
        [TestMethod]
        public void CannotCreateArticleWithNegativePriceThrows() =>
            // Test happens at Money layer (correct place for invariant)
            Assert.ThrowsException<DomainException>(() => Article.Create(1, "ART001", "Valid Name", -10.0m, "CHF", 1));

        // ============================================================
        // 3) ChangePrice(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void ChangePriceValidPriceShouldSucceedAndRaiseEvent()
        {
            Result<Article> r = CreateValidArticle();
            Article a = r.Value!;

            Result<Money> moneyResult = Money.From(199.99m, "CHF")!;
            Assert.IsTrue(moneyResult.IsSuccess);
            Money newPrice = moneyResult.Value!;
            Result result = a.ChangePrice(newPrice);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(199.99m, a.Price.Amount);
            Assert.IsTrue(a.DomainEvents.Any(e => e is ArticlePriceChanged));
        }

        // ============================================================
        // 4) UpdateStock(...) — Equivalence Classes & BVA
        // ============================================================

        [TestMethod]
        public void UpdateStockIncreaseShouldSucceed()
        {
            Result<Article> r = CreateValidArticle(stock: 10);
            Article a = r.Value!;

            Result result = a.UpdateStock(5);  // Increase

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(15, a.Stock);
            Assert.IsTrue(a.DomainEvents.Any(e => e is ArticleStockChanged));
        }

        [TestMethod]
        public void UpdateStockDecreaseToZeroShouldSucceed()
        {
            Result<Article> r = CreateValidArticle(stock: 5);
            Article a = r.Value!;

            Result result = a.UpdateStock(-5);  // To zero

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(0, a.Stock);
        }

        [TestMethod]
        public void UpdateStockDecreaseBelowZeroShouldFail()
        {
            Result<Article> r = CreateValidArticle(stock: 3);
            Article a = r.Value!;

            Result result = a.UpdateStock(-5);  // Below zero

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(3, a.Stock);  // Unchanged
        }

        // ============================================================
        // 5) ChangeGroup(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void ChangeGroupValidGroupIdShouldSucceedAndRaiseEvent()
        {
            Result<Article> r = CreateValidArticle();
            Article a = r.Value!;

            Result result = a.ChangeGroup(new ArticleGroupId(2));

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2, a.ArticleGroupId.Value);
            Assert.IsTrue(a.DomainEvents.Any(e => e is ArticleMovedToGroup));
        }

        [TestMethod]
        public void ChangeGroupInvalidGroupIdShouldFail()
        {
            Result<Article> r = CreateValidArticle();
            Article a = r.Value!;

            Result result = a.ChangeGroup(new ArticleGroupId(-1));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreNotEqual(-1, a.ArticleGroupId.Value);
        }

        // ============================================================
        // 6) CreateInvalidCurrencyThrowsDomainException()
        // ============================================================

        [TestMethod]
        public void CreateInvalidCurrencyThrowsDomainException()
        {
            // ECP: Invalid currency (Money.From throws)
            _ = Assert.ThrowsException<DomainException>(() =>
                Article.Create(1, "ART001", "Valid Name", 10.0m, "XX", 1));

            // BVA: Wrong length currencies
            _ = Assert.ThrowsException<DomainException>(() =>
                Article.Create(1, "ART001", "Valid Name", 10.0m, "CH", 1));   // 2 chars
            _ = Assert.ThrowsException<DomainException>(() =>
                Article.Create(1, "ART001", "Valid Name", 10.0m, "CHHH", 1)); // 4 chars
        }

    }
}
