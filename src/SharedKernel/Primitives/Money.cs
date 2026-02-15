using SharedKernel.SeedWork;

namespace SharedKernel.Primitives
{
    public sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        private Money(decimal amount, string currency)
        {
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            Currency = currency;
        }

        public static Result<Money> From(decimal amount, string currency)
        {
            if (amount < 0)
                return Results.Fail<Money>("Amount cannot be negative.");

            if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
                return Results.Fail<Money>("Currency must be a 3-letter code.");

            var money = new Money(amount, currency.Trim().ToUpperInvariant());
            return Results.Success(money);
        }

        public static Money operator +(Money left, Money right)
        {
            left.EnsureSameCurrency(right).EnsureSuccess();
            return new Money(left.Amount + right.Amount, left.Currency);
        }

        public static Money operator -(Money left, Money right)
        {
            left.EnsureSameCurrency(right).EnsureSuccess();
            return new Money(left.Amount - right.Amount, left.Currency);
        }

        public static Money operator *(Money left, int multiplier)
            => new(left.Amount * multiplier, left.Currency);

        public static Money operator /(Money dividend, int divisor)
        {
            Result<Money> result = divisor == 0
                ? Results.Fail<Money>("Cannot divide by zero.")
                : Results.Success(new Money(dividend.Amount / divisor, dividend.Currency));

            return result.EnsureValue();
        }

        public Money Add(Money other) => this + other;
        public Money Subtract(Money other) => this - other;
        public Money Multiply(int factor) => this * factor;
        public Money Divide(int divisor) => this / divisor;

        private Result EnsureSameCurrency(Money other) =>
            !string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase)
                ? Result.Fail("Currency mismatch.")
                : Result.Success();

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        public override string ToString() => $"{Amount:0.00} {Currency}";
    }
}
