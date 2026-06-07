using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Orders
{
    public sealed class OrderLine : Entity<OrderLineId>
    {
        private OrderLine() : base(OrderLineId.Empty)
        {
            // EF Core
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
        public string ArticleName { get; private set; } = default!;
        public Money UnitPrice { get; private set; } = default!;
        public int Quantity { get; private set; }
        public Money LineTotal { get; private set; } = default!;
    }
}
