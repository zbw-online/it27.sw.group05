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
            DateOnly deliveryDate,
            Address billingAddress,
            AddressSource billingAddressSource,
            Address deliveryAddress,
            AddressSource deliveryAddressSource,
            string? customerReference,
            TimeProvider timeProvider)
            : base(OrderId.Empty)
        {
            OrderNumber = number;
            OrderDate = timeProvider.GetUtcNow().UtcDateTime;
            CustomerId = customerId;
            DeliveryDate = deliveryDate;
            BillingAddress = billingAddress;
            BillingAddressSource = billingAddressSource;
            DeliveryAddress = deliveryAddress;
            DeliveryAddressSource = deliveryAddressSource;
            CustomerReference = customerReference;
            Total = Money.From(0, "CHF").EnsureValue();
            IsInventoryApplied = false;

            AddDomainEvent(new OrderCreated(number, DateTime.UtcNow));
        }

        public OrderNumber OrderNumber { get; private set; } = default!;
        public DateTime OrderDate { get; private set; }
        public CustomerId CustomerId { get; private set; }
        public DateOnly DeliveryDate { get; private set; }
        public Address BillingAddress { get; private set; } = default!;
        public AddressSource BillingAddressSource { get; private set; }
        public Address DeliveryAddress { get; private set; } = default!;
        public AddressSource DeliveryAddressSource { get; private set; }
        public string? CustomerReference { get; private set; }
        public Money Total { get; private set; } = default!;
        public bool IsInventoryApplied { get; private set; }
        public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

        public static Result<Order> Create(
            string orderNumber,
            CustomerId customerId,
            DateOnly deliveryDate,
            Address billingAddress,
            AddressSource billingAddressSource,
            Address deliveryAddress,
            AddressSource deliveryAddressSource,
            string? customerReference = null,
            TimeProvider? timeProvider = null)
        {
            Result<OrderNumber> nr = OrderNumber.Create(orderNumber);
            if (!nr.IsSuccess)
                return Results.Fail<Order>(nr.Error!);

            if (!customerId.IsAssigned)
                return Results.Fail<Order>("CustomerId must be assigned before creating an order.");

            string? normalizedReference = string.IsNullOrWhiteSpace(customerReference)
                ? null
                : customerReference.Trim();

            if (normalizedReference is { Length: > 100 })
                return Results.Fail<Order>("CustomerReference must not exceed 100 characters.");

            var order = new Order(
                nr.Value!,
                customerId,
                deliveryDate,
                billingAddress,
                billingAddressSource,
                deliveryAddress,
                deliveryAddressSource,
                normalizedReference,
                timeProvider ?? TimeProvider.System);

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

            int nextLineNumber = _lines.Count == 0 ? 1 : _lines.Max(x => x.LineNumber) + 1;

            var line = new OrderLine(
                OrderLineId.Empty,
                nextLineNumber,
                articleId,
                articleName.Trim(),
                unitPrice,
                quantity);

            _lines.Add(line);

            RecalculateTotal();
            return Result.Success();
        }

        public Result UpdateLineQuantity(OrderLineId lineId, int quantity)
        {
            OrderLine? line = _lines.FirstOrDefault(x => x.Id == lineId);
            if (line is null)
                return Result.Fail("Order line was not found.");

            Result result = line.ChangeQuantity(quantity);
            if (!result.IsSuccess)
                return result;

            RecalculateTotal();
            return Result.Success();
        }

        public Result RemoveLine(OrderLineId lineId)
        {
            OrderLine? line = _lines.FirstOrDefault(x => x.Id == lineId);
            if (line is null)
                return Result.Fail("Order line was not found.");

            _ = _lines.Remove(line);

            RecalculateTotal();
            return Result.Success();
        }

        private void RecalculateTotal()
        {
            decimal totalAmount = _lines.Sum(x => x.LineTotal.Amount);
            string currency = _lines.FirstOrDefault()?.LineTotal.Currency ?? "CHF";

            Total = Money.From(totalAmount, currency).EnsureValue();
        }

        public Result MarkInventoryApplied()
        {
            if (IsInventoryApplied)
                return Result.Fail("Inventory has already been applied for this order.");

            IsInventoryApplied = true;
            return Result.Success();
        }
    }
}
