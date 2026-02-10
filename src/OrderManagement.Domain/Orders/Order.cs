using System.Net;

using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Domain.Orders.Events;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;
using SharedKernel.ValueObjects;

namespace OrderManagement.Domain.Orders;

public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = new();
    private int _lineIdSeq;

    private Order() : base(default!) { }

    private Order(OrderId id, string orderNumber, CustomerId customerId, Address deliveryAddress)
        : base(id)
    {
        OrderNumber = orderNumber;
        OrderDate = DateTime.UtcNow;
        CustomerId = customerId;
        DeliveryAddress = deliveryAddress;
        Total = Money.From(0, "CHF")!; // Standardwährung
    }

    public string OrderNumber { get; private set; } = default!;
    public DateTime OrderDate { get; private set; }
    public CustomerId CustomerId { get; private set; } = default!;
    public Address DeliveryAddress { get; private set; } = default!;
    public Money Total { get; private set; } = default!;
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Result<Order> Create(int id, string orderNumber, CustomerId customerId, Address deliveryAddress)
    {
        if (id <= 0) return Results.Fail<Order>("Order ID must be positive.");
        if (string.IsNullOrWhiteSpace(orderNumber)) return Results.Fail<Order>("Order number is required.");

        var order = new Order(new OrderId(id), orderNumber, customerId, deliveryAddress);
        order.AddDomainEvent(new OrderCreated(order.Id, DateTime.UtcNow));

        return Results.Success(order);
    }
    public Result AddLine(int articleId, string articleName, Money unitPrice, int quantity)
    {
        if (quantity <= 0) return Result.Fail("Quantity must be positive.");

        if (_lines.Any() && _lines[0].UnitPrice.Currency != unitPrice.Currency)
        {
            return Result.Fail($"Invalid currency. Expected {_lines[0].UnitPrice.Currency} but got {unitPrice.Currency}.");
        }

        _lineIdSeq++;
        var line = new OrderLine(
            new OrderLineId(_lineIdSeq),
            _lines.Count + 1,
            articleId,
            articleName,
            unitPrice,
            quantity);

        _lines.Add(line);

        RecalculateTotal();
        return Result.Success();
    }

    private void RecalculateTotal()
    {
        var totalAmount = _lines.Sum(x => x.LineTotal.Amount);
        var currency = _lines.FirstOrDefault()?.LineTotal.Currency ?? "CHF";
        Total = Money.From(totalAmount, currency)!;
    }
}
