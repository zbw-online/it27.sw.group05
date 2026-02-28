using System.Text.RegularExpressions;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;


namespace OrderManagement.Domain.Customers.ValueObjects
{
    public sealed partial class CustomerNumber : ValueObject
    {

        private static readonly Regex Pattern = MyRegex();

        public string Value { get; }

        private CustomerNumber(string value) => Value = value;

        // For EFCore (Not sure if this is the Correct approach)
        internal static CustomerNumber FromDb(string value) => new(value);

        public static Result<CustomerNumber> Create(string? input)
        {
            string value = (input ?? string.Empty).Trim().ToUpperInvariant();

            return value.Length == 0
                ? Results.Fail<CustomerNumber>("Customer number is required.")
                : !Pattern.IsMatch(value)
                ? Results.Fail<CustomerNumber>("Customer number must match format 'C-00001'.")
                : Results.Success(new CustomerNumber(value));
        }


        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;
        [GeneratedRegex(@"^C-\d{5}$", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
    }
}
