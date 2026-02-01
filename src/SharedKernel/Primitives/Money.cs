using SharedKernel.SeedWork;

namespace SharedKernel.Primitives
{
    public sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        private Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public static Money From(decimal amount, string currency) => amount < 0
                ? throw new DomainException("Amount cannot be negative.")
                : string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3
                ? throw new DomainException("Currency must be a 3-letter code.")
                : new Money(
                decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
                currency.Trim().ToUpperInvariant());

        public static Money operator +(Money left, Money right)
        {
            left.EnsureSameCurrency(right);
            return new Money(left.Amount + right.Amount, left.Currency);
        }

        public static Money operator -(Money left, Money right)
        {
            left.EnsureSameCurrency(right);
            return new Money(left.Amount - right.Amount, left.Currency);
        }

        public static Money operator *(Money left, int multiplier)
            => new(left.Amount * multiplier, left.Currency);

        public static Money operator /(Money dividend, int divisor) => divisor == 0
                ? throw new DomainException("Cannot divide by zero.")
                : new Money(
                decimal.Round(dividend.Amount / divisor, 2, MidpointRounding.AwayFromZero),
                dividend.Currency);

        public Money Add(Money other) => this + other;
        public Money Subtract(Money other) => this - other;
        public Money Multiply(int factor) => this * factor;
        public Money Divide(int divisor) => this / divisor;

        private void EnsureSameCurrency(Money other)
        {
            if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
                throw new DomainException("Currency mismatch.");
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        public override string ToString() => $"{Amount:0.00} {Currency}";
    }
}
