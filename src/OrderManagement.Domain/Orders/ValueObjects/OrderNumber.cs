using System.Text.RegularExpressions;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;


namespace OrderManagement.Domain.Orders.ValueObjects
{
    public sealed partial class OrderNumber : ValueObject
    {
        private static readonly Regex Pattern = MyRegex();

        public string Value { get; }

        private OrderNumber(string value) => Value = value;

        public static Result<OrderNumber> Create(string? input)
        {
            string value = (input ?? string.Empty).Trim().ToUpperInvariant();

            return value.Length == 0
                ? Results.Fail<OrderNumber>("Order number is required.")
                : !Pattern.IsMatch(value)
                ? Results.Fail<OrderNumber>("Order number must match format 'ORD-2025-001'.")
                : Results.Success(new OrderNumber(value));
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        [GeneratedRegex(@"^ORD-\d{4}-\d{3}$", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
    }
}

