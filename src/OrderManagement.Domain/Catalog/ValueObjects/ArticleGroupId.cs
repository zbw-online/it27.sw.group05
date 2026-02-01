using System.Globalization;

namespace OrderManagement.Domain.Catalog.ValueObjects
{
    public readonly record struct ArticleGroupId(int Value)
    {
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
}
