using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders.Events;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Orders
{
    public sealed class Order : AggregateRoot<OrderId>
    {
        private readonly List<OrderLine> _lines = [];
        private int _lineIdSeq;

        private Order() : base(new OrderId(0)) { }

        private Order(
            OrderId id,
            OrderNumber number,
            CustomerId customerId,
            Address deliveryAddress)
            : base(id)
        {
            OrderNumber = number;
            OrderDate = DateTime.UtcNow;
            CustomerId = customerId;
            DeliveryAddress = deliveryAddress;

            Total = Money.From(0, "CHF").EnsureValue();

            // IMPORTANT: match constructor signature of your event
            AddDomainEvent(new OrderCreated(id, DateTime.UtcNow));
            // If your event only takes id: AddDomainEvent(new OrderCreated(id));
        }

        public OrderNumber OrderNumber { get; private set; } = default!;
        public DateTime OrderDate { get; private set; }
        public CustomerId CustomerId { get; private set; } = default!;
        public Address DeliveryAddress { get; private set; } = default!;
        public Money Total { get; private set; } = default!;
        public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

        public static Result<Order> Create(
            int id,
            string orderNumber,
            CustomerId customerId,
            Address deliveryAddress)
        {
            if (id <= 0)
                return Results.Fail<Order>("Order ID must be positive.");

            Result<OrderNumber> nr = OrderNumber.Create(orderNumber);
            if (!nr.IsSuccess)
                return Results.Fail<Order>(nr.Error!);

            // You can add additional rules here (e.g., deliveryAddress not null)

            var order = new Order(
                new OrderId(id),
                nr.Value!,
                customerId,
                deliveryAddress);

            return Results.Success(order);
        }

        public Result AddLine(int articleId, string articleName, Money unitPrice, int quantity)
        {
            if (quantity <= 0) return Result.Fail("Quantity must be positive.");

            if (_lines.Count != 0 && _lines[0].UnitPrice.Currency != unitPrice.Currency)
                return Result.Fail($"Invalid currency. Expected {_lines[0].UnitPrice.Currency} but got {unitPrice.Currency}.");

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
            decimal totalAmount = _lines.Sum(x => x.LineTotal.Amount);
            string currency = _lines.FirstOrDefault()?.LineTotal.Currency ?? "CHF";
            Total = Money.From(totalAmount, currency).EnsureValue();
        }
    }
}
