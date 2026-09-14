using System.Globalization;

namespace OrderManagement.Domain.Orders.ValueObjects
{
    public readonly record struct OrderLineId(int Value)
    {
        public static OrderLineId Empty => new(0);
        public bool IsAssigned => Value > 0;
        public override string ToString()
            => Value.ToString(CultureInfo.InvariantCulture);
    }
}
