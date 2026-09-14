using System.Globalization;

namespace OrderManagement.Domain.Catalog.ValueObjects
{
    public readonly record struct ArticleId(int Value)
    {
        public static ArticleId Empty => new(0);
        public bool IsAssigned => Value > 0;
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}
