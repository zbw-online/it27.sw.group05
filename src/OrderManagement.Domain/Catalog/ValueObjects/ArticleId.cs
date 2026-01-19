using System.Globalization;

namespace OrderManagement.Domain.Catalog.ValueObjects
{
    public readonly record struct ArticleId(int Value)
    {
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}
