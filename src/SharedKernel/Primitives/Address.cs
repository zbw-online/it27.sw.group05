using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace SharedKernel.ValueObjects;

public sealed class Address : ValueObject
{
    private Address(string street, string number, string postalCode, string city, string country)
    {
        Street = street;
        Number = number;
        PostalCode = postalCode;
        City = city;
        Country = country;
    }

    public string Street { get; }
    public string Number { get; }
    public string PostalCode { get; }
    public string City { get; }
    public string Country { get; }

    public static Result<Address> Create(string street, string number, string postalCode, string city, string country)
    {
        string s = (street ?? string.Empty).Trim();
        string n = (number ?? string.Empty).Trim();
        string pc = (postalCode ?? string.Empty).Trim();
        string ci = (city ?? string.Empty).Trim();
        string co = (country ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(s)) return Results.Fail<Address>("Street is required.");
        if (string.IsNullOrEmpty(n)) return Results.Fail<Address>("House number is required.");
        if (string.IsNullOrEmpty(pc)) return Results.Fail<Address>("Postal code is required.");
        if (string.IsNullOrEmpty(ci)) return Results.Fail<Address>("City is required.");

        if (co.Length != 2)
            return Results.Fail<Address>("Country must be a 2-letter ISO code (e.g., CH).");

        if (pc.Length < 3 || pc.Length > 10)
            return Results.Fail<Address>("Postal code has an invalid length.");

        return Results.Success(new Address(s, n, pc, ci, co));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return Number;
        yield return PostalCode;
        yield return City;
        yield return Country;
    }
}
