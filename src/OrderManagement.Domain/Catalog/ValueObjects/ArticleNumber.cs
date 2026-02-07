using System.Text.RegularExpressions;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.ValueObjects
{
    public sealed partial class ArticleNumber : ValueObject
    {
        private static readonly Regex Pattern = MyRegex();

        public string Value { get; }

        private ArticleNumber(string value) => Value = value;

        public static Result<ArticleNumber> Create(string? input)
        {
            string value = (input ?? string.Empty).Trim().ToUpperInvariant();

            return value.Length == 0
                ? Results.Fail<ArticleNumber>("Article number is required.")
                : value.Length > 20
                ? Results.Fail<ArticleNumber>("Article number must be at most 20 characters.")
                : !Pattern.IsMatch(value)
                ? Results.Fail<ArticleNumber>("Article number has an invalid format.")
                : Results.Success(new ArticleNumber(value));
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        [GeneratedRegex("^[A-Z0-9\\-]{1,20}$", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
    }
}
