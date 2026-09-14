using System.Globalization;

namespace OrderManagement.Domain.Orders.ValueObjects
{
    public readonly record struct OrderId(int Value)
    {
        public static OrderId Empty => new(0);
        public bool IsAssigned => Value > 0;
        public override string ToString()
            => Value.ToString(CultureInfo.InvariantCulture);
    }
}
