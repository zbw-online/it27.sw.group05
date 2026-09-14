using System.Globalization;

namespace OrderManagement.Domain.Catalog.ValueObjects
{
    public readonly record struct ArticleGroupId(int Value)
    {
        public static ArticleGroupId Empty => new(0);
        public bool IsAssigned => Value > 0;
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}
