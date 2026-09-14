namespace OrderManagement.Application.Features.Customers.CreateCustomer
{
    public sealed record CreateCustomerCommand(
        string CustomerNumber,
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
