using SharedKernel.Primitives;
using SharedKernel.SeedWork;

using System.Text.RegularExpressions;

namespace SharedKernel.ValueObjects;

public sealed class Money : ValueObject
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return Results.Fail<Money>("Currency code is required.");

        string cleanedCurrency = currency.Trim().ToUpperInvariant();
        if (cleanedCurrency.Length != 3 || !Regex.IsMatch(cleanedCurrency, @"^[A-Z]{3}$"))
            return Results.Fail<Money>("Currency code must be a 3-letter ISO code (e.g., CHF, EUR).");

        if (amount < 0)
            return Results.Fail<Money>("Amount cannot be negative.");

        return Results.Success(new Money(amount, cleanedCurrency));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public Result<Money> Add(Money other)
    {
        if (this.Currency != other.Currency)
            return Results.Fail<Money>("Cannot add different currencies.");

        return Create(this.Amount + other.Amount, this.Currency);
    }
}
