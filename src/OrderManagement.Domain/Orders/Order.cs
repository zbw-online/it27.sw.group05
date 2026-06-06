using OrderManagement.Domain.Catalog.ValueObjects;
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

        private Order() : base(OrderId.Empty)
        {
            // EF Core
        }

        private Order(
            OrderNumber number,
            CustomerId customerId,
            Address deliveryAddress)
            : base(OrderId.Empty)
        {
            OrderNumber = number;
            OrderDate = DateTime.UtcNow;
            CustomerId = customerId;
            DeliveryAddress = deliveryAddress;
            Total = Money.From(0, "CHF").EnsureValue();

            AddDomainEvent(new OrderCreated(number, DateTime.UtcNow));
        }

        public OrderNumber OrderNumber { get; private set; } = default!;
        public DateTime OrderDate { get; private set; }
        public CustomerId CustomerId { get; private set; }
        public Address DeliveryAddress { get; private set; } = default!;
        public Money Total { get; private set; } = default!;
        public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

        public static Result<Order> Create(
            string orderNumber,
            CustomerId customerId,
            Address deliveryAddress)
        {
            Result<OrderNumber> nr = OrderNumber.Create(orderNumber);
            if (!nr.IsSuccess)
                return Results.Fail<Order>(nr.Error!);

            if (!customerId.IsAssigned)
                return Results.Fail<Order>("CustomerId must be assigned before creating an order.");


            var order = new Order(
                nr.Value!,
                customerId,
                deliveryAddress);

            return Results.Success(order);
        }

        public Result AddLine(
            ArticleId articleId,
            string articleName,
            Money unitPrice,
            int quantity)
        {
            if (!articleId.IsAssigned)
                return Result.Fail("ArticleId must be assigned");

            if (string.IsNullOrWhiteSpace(articleName))
                return Result.Fail("ArticleName is required.");

            if (quantity <= 0)
                return Result.Fail("Quantity must be positive.");

            if (_lines.Count != 0 && _lines[0].UnitPrice.Currency != unitPrice.Currency)
                return Result.Fail($"Invalid currency. Expected {_lines[0].UnitPrice.Currency} but got {unitPrice.Currency}.");

            var line = new OrderLine(
                OrderLineId.Empty,
                _lines.Count + 1,
                articleId,
                articleName.Trim(),
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
