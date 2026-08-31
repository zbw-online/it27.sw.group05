using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.Events;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Domain.Tests.Catalog
{
    [TestClass]
    public sealed class ArticleTests
    {
        private static readonly ArticleGroupId ValidGroupId = new(1);

        private static Result<Article> CreateValidArticle(
            string? articleNr = "ART-000001",
            string? name = "Test Article",
            decimal priceAmount = 99.99m,
            string priceCurrency = "CHF",
            ArticleGroupId? groupId = null,
            int stock = 10,
            decimal vatRate = 7.70m,
            string? description = "Test description",
            ArticleStatus status = ArticleStatus.Active) => Article.Create(
                articleNr: articleNr,
                name: name,
                priceAmount: priceAmount,
                priceCurrency: priceCurrency,
                groupId: groupId ?? ValidGroupId,
                stock: stock,
                vatRate: vatRate,
                description: description,
                status: status);

        [TestMethod]
        public void Create_WithValidInputs_ShouldSucceed()
        {
            Result<Article> result = CreateValidArticle();

            Assert.IsTrue(result.IsSuccess, result.Error);

            Article article = result.Value!;

            Assert.AreEqual(0, article.Id.Value);
            Assert.AreEqual("ART-000001", article.ArticleNumber.Value);
            Assert.AreEqual("Test Article", article.Name);
            Assert.AreEqual(99.99m, article.Price.Amount);
            Assert.AreEqual("CHF", article.Price.Currency);
            Assert.AreEqual(ValidGroupId, article.ArticleGroupId);
            Assert.AreEqual(10, article.Stock);
            Assert.AreEqual(7.70m, article.VatRate);
            Assert.AreEqual("Test description", article.Description);
            Assert.AreEqual(ArticleStatus.Active, article.Status);
        }

        [TestMethod]
        public void Create_WithValidInputs_ShouldRaiseCreatedEvent()
        {
            Article article = CreateValidArticle().EnsureValue();

            Assert.IsTrue(article.DomainEvents.Any(e => e is ArticleCreated));
        }

        [TestMethod]
        public void Create_WithInvalidArticleNumber_ShouldFail()
        {
            Result<Article> result = CreateValidArticle(articleNr: "INVALID ARTICLE NUMBER!");

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Create_WithWhitespaceName_ShouldFail()
        {
            Result<Article> result = CreateValidArticle(name: "   ");

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Create_WithNameLongerThan200Characters_ShouldFail()
        {
            string name = new('A', 201);

            Result<Article> result = CreateValidArticle(name: name);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Create_WithUnassignedArticleGroupId_ShouldFail()
        {
            Result<Article> result = CreateValidArticle(groupId: ArticleGroupId.Empty);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Create_WithNegativeStock_ShouldFail()
        {
            Result<Article> result = CreateValidArticle(stock: -1);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Create_WithNegativeVatRate_ShouldFail()
        {
            Result<Article> result = CreateValidArticle(vatRate: -0.01m);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Create_WithVatRateGreaterThan99999_ShouldFail()
        {
            Result<Article> result = CreateValidArticle(vatRate: 1000.00m);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Create_WithVatRateMoreThanTwoDecimals_ShouldFail()
        {
            Result<Article> result = CreateValidArticle(vatRate: 7.777m);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void ChangePrice_WithValidPrice_ShouldSucceed()
        {
            Article article = CreateValidArticle().EnsureValue();
            Money newPrice = Money.From(199.99m, "CHF").EnsureValue();

            Result result = article.ChangePrice(newPrice);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(199.99m, article.Price.Amount);
            Assert.AreEqual("CHF", article.Price.Currency);
        }

        [TestMethod]
        public void ChangePrice_WithValidPrice_ShouldRaisePriceChangedEvent()
        {
            Article article = CreateValidArticle().EnsureValue();
            Money newPrice = Money.From(199.99m, "CHF").EnsureValue();

            Result result = article.ChangePrice(newPrice);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(article.DomainEvents.Any(e => e is ArticlePriceChanged));
        }

        [TestMethod]
        public void UpdateStock_WithPositiveDelta_ShouldIncreaseStock()
        {
            Article article = CreateValidArticle(stock: 10).EnsureValue();

            Result result = article.UpdateStock(5);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(15, article.Stock);
        }

        [TestMethod]
        public void UpdateStock_WithNegativeDeltaWithinAvailableStock_ShouldDecreaseStock()
        {
            Article article = CreateValidArticle(stock: 10).EnsureValue();

            Result result = article.UpdateStock(-4);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(6, article.Stock);
        }

        [TestMethod]
        public void UpdateStock_DecreaseBelowZero_ShouldFail()
        {
            Article article = CreateValidArticle(stock: 3).EnsureValue();

            Result result = article.UpdateStock(-5);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(3, article.Stock);
        }

        [TestMethod]
        public void UpdateStock_WithValidDelta_ShouldRaiseStockChangedEvent()
        {
            Article article = CreateValidArticle(stock: 10).EnsureValue();

            Result result = article.UpdateStock(5);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(article.DomainEvents.Any(e => e is ArticleStockChanged));
        }

        [TestMethod]
        public void ChangeGroup_WithAssignedGroupId_ShouldSucceed()
        {
            Article article = CreateValidArticle().EnsureValue();
            var newGroupId = new ArticleGroupId(2);

            Result result = article.ChangeGroup(newGroupId);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(newGroupId, article.ArticleGroupId);
        }

        [TestMethod]
        public void ChangeGroup_WithUnassignedGroupId_ShouldFail()
        {
            Article article = CreateValidArticle().EnsureValue();

            Result result = article.ChangeGroup(ArticleGroupId.Empty);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(ValidGroupId, article.ArticleGroupId);
        }

        [TestMethod]
        public void ChangeGroup_WithAssignedGroupId_ShouldRaiseMovedEvent()
        {
            Article article = CreateValidArticle().EnsureValue();
            var newGroupId = new ArticleGroupId(2);

            Result result = article.ChangeGroup(newGroupId);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(article.DomainEvents.Any(e => e is ArticleMovedToGroup));
        }

        [TestMethod]
        public void Deactivate_WhenActive_ShouldSucceedAndSetInactive()
        {
            Article article = CreateValidArticle().EnsureValue();

            Result result = article.Deactivate();

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(ArticleStatus.Inactive, article.Status);
        }

        [TestMethod]
        public void Deactivate_WhenActive_ShouldRaiseDeactivatedEvent()
        {
            Article article = CreateValidArticle().EnsureValue();

            Result result = article.Deactivate();

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(article.DomainEvents.Any(e => e is ArticleDeactivated));
        }

        [TestMethod]
        public void Deactivate_WhenAlreadyInactive_ShouldFail()
        {
            Article article = CreateValidArticle().EnsureValue();
            article.Deactivate().EnsureSuccess();

            Result result = article.Deactivate();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(ArticleStatus.Inactive, article.Status);
        }

        [TestMethod]
        public void EnsureAvailableForOrder_WhenActive_ShouldSucceed()
        {
            Article article = CreateValidArticle().EnsureValue();

            Result result = article.EnsureAvailableForOrder();

            Assert.IsTrue(result.IsSuccess, result.Error);
        }

        [TestMethod]
        public void EnsureAvailableForOrder_WhenInactive_ShouldFailWithArticleName()
        {
            Article article = CreateValidArticle().EnsureValue();
            article.Deactivate().EnsureSuccess();

            Result result = article.EnsureAvailableForOrder();

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, article.Name);
        }

        [TestMethod]
        public void Reactivate_WhenInactive_ShouldSucceedAndSetActive()
        {
            Article article = CreateValidArticle().EnsureValue();
            article.Deactivate().EnsureSuccess();

            Result result = article.Reactivate();

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(ArticleStatus.Active, article.Status);
        }

        [TestMethod]
        public void Reactivate_WhenInactive_ShouldRaiseReactivatedEvent()
        {
            Article article = CreateValidArticle().EnsureValue();
            article.Deactivate().EnsureSuccess();

            Result result = article.Reactivate();

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(article.DomainEvents.Any(e => e is ArticleReactivated));
        }

        [TestMethod]
        public void Reactivate_WhenAlreadyActive_ShouldFail()
        {
            Article article = CreateValidArticle().EnsureValue();

            Result result = article.Reactivate();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(ArticleStatus.Active, article.Status);
        }
    }
}
