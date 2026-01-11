

using System.Globalization;

namespace OrderManagement.Domain.Customers.ValueObjects
{
    public readonly record struct CustomerId(int Value)
    {
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}
