using System.Text.RegularExpressions;

using SharedKernel.SeedWork;

namespace SharedKernel.Primitives
{
    public sealed partial class Email : ValueObject
    {

        private static readonly Regex EmailRegex = MyRegex();

        public string Value { get; }
        private Email(string value) => Value = value;

        public static Result<Email> Create(string? input)
        {
            string value = (input ?? string.Empty).Trim();

            return value.Length == 0
                ? Results.Fail<Email>("Email is required.")
                : value.Length > 255
                ? Results.Fail<Email>("Email is too long.")
                : !EmailRegex.IsMatch(value)
                ? Results.Fail<Email>("Email format is invalid.")
                : Results.Success(new Email(value.ToLowerInvariant()));
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;
        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex MyRegex();
    }
}
