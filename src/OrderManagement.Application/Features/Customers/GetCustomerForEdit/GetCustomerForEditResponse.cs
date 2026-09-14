namespace OrderManagement.Application.Features.Customers.GetCustomerForEdit
{
    public sealed record GetCustomerForEditResponse(
        int CustomerId,
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
