using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;
using SharedKernel.ValueObjects;

namespace OrderManagement.Domain.Orders;

public sealed class OrderLine : Entity<OrderLineId>
{

    internal OrderLine(
        OrderLineId id,
        int lineNumber,
        int articleId,
        string articleName,
        Money unitPrice,
        int quantity) : base(id)
    {
        LineNumber = lineNumber;
        ArticleId = articleId;
        ArticleName = articleName;
        UnitPrice = unitPrice;
        Quantity = quantity;

        LineTotal = Money.Create(unitPrice.Amount * quantity, unitPrice.Currency).Value!;
    }

    public int LineNumber { get; private set; }
    public int ArticleId { get; private set; }
    public string ArticleName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public Money LineTotal { get; private set; }
}
