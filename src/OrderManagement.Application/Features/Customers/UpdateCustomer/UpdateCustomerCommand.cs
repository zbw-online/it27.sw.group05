namespace OrderManagement.Application.Features.Customers.UpdateCustomer
{
    public sealed record UpdateCustomerCommand(
        int CustomerId,
        string LastName,
        string SurName,
        string Email,
        string? Website,
        DateOnly AddressValidFrom,
        string Street,
        string HouseNumber,
        string PostalCode,
        string City,
        string CountryCode);
}
