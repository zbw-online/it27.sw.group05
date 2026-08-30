using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.Shared;
using OrderManagement.Presentation.Blazor.Components.Pages.Orders;

namespace OrderManagement.Presentation.Blazor.Tests.Orders
{
    [TestClass]
    public sealed class OrderCartTests
    {
        private static ArticleListItemDto Article(int id = 1, decimal price = 10m, decimal vatRate = 8.1m, int stock = 50) =>
            new(id, $"ART-{id}", $"Artikel {id}", price, "CHF", 1, "Gruppe", stock, vatRate, 1);

        [TestMethod]
        public void Add_NewArticle_CreatesLineWithQuantityOne()
        {
            OrderCart cart = new();

            cart.Add(Article(1));

            Assert.AreEqual(1, cart.Lines.Count);
            Assert.AreEqual(1, cart.Lines[0].Quantity);
        }

        [TestMethod]
        public void Add_SameArticleTwice_MergesIntoOneLineWithIncrementedQuantity()
        {
            OrderCart cart = new();

            cart.Add(Article(1));
            cart.Add(Article(1));

            Assert.AreEqual(1, cart.Lines.Count);
            Assert.AreEqual(2, cart.Lines[0].Quantity);
        }

        [TestMethod]
        public void ChangeQuantity_Increment_IncreasesQuantity()
        {
            OrderCart cart = new();
            cart.Add(Article(1));

            cart.ChangeQuantity(1, 1);

            Assert.AreEqual(2, cart.Lines[0].Quantity);
        }

        [TestMethod]
        public void ChangeQuantity_BelowOne_IsRejected()
        {
            OrderCart cart = new();
            cart.Add(Article(1));

            cart.ChangeQuantity(1, -1);

            Assert.AreEqual(1, cart.Lines[0].Quantity);
        }

        [TestMethod]
        public void SetQuantity_InvalidValue_IsIgnored()
        {
            OrderCart cart = new();
            cart.Add(Article(1));

            cart.SetQuantity(1, 0);

            Assert.AreEqual(1, cart.Lines[0].Quantity);
        }

        [TestMethod]
        public void SetQuantity_ValidValue_UpdatesQuantity()
        {
            OrderCart cart = new();
            cart.Add(Article(1));

            cart.SetQuantity(1, 5);

            Assert.AreEqual(5, cart.Lines[0].Quantity);
        }

        [TestMethod]
        public void Remove_ExistingLine_RemovesIt()
        {
            OrderCart cart = new();
            cart.Add(Article(1));

            cart.Remove(1);

            Assert.AreEqual(0, cart.Lines.Count);
        }

        [TestMethod]
        public void Totals_RecalculateAfterEachMutation()
        {
            OrderCart cart = new();

            cart.Add(Article(1, price: 10m, vatRate: 10m));
            Assert.AreEqual(10m, cart.Totals.Subtotal);

            cart.ChangeQuantity(1, 1);
            Assert.AreEqual(20m, cart.Totals.Subtotal);
            Assert.AreEqual(2m, cart.Totals.VatAmount);
            Assert.AreEqual(22m, cart.Totals.Total);

            cart.Remove(1);
            Assert.AreEqual(0m, cart.Totals.Subtotal);
        }
    }
}
