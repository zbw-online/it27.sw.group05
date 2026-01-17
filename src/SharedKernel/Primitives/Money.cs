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

        public Money Add(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount - other.Amount, Currency);
        }

        public Money Multiply(int factor)
            => new(Amount * factor, Currency);

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
