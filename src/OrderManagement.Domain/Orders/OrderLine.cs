using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Orders
{
    public sealed class OrderLine : Entity<OrderLineId>
    {

        // EF Core only (materialization)
        private OrderLine() : base(default!)
        {
            ArticleName = null!;
            UnitPrice = null!;
            LineTotal = null!;
        }

        internal OrderLine(
            OrderLineId id,
            int lineNumber,
            ArticleId articleId,
            string articleName,
            Money unitPrice,
            int quantity) : base(id)
        {
            LineNumber = lineNumber;
            ArticleId = articleId;
            ArticleName = articleName;
            UnitPrice = unitPrice;
            Quantity = quantity;

            LineTotal = Money.From(unitPrice.Amount * quantity, unitPrice.Currency).EnsureValue();
        }

        public int LineNumber { get; private set; }
        public ArticleId ArticleId { get; private set; }
        public string ArticleName { get; private set; }
        public Money UnitPrice { get; private set; }
        public int Quantity { get; private set; }
        public Money LineTotal { get; private set; }
    }
}
