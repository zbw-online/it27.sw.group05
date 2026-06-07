

using System.Globalization;

namespace OrderManagement.Domain.Customers.ValueObjects
{
    public readonly record struct CustomerId(int Value)
    {
        public static CustomerId Empty => new(0);

        public bool IsAssigned => Value > 0;
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}
