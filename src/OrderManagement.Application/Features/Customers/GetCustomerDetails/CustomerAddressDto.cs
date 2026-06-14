namespace OrderManagement.Application.Features.Customers.GetCustomerDetails
{
    public sealed record CustomerAddressDto(
        int CustomerAddressId,
        DateOnly ValidFrom,
        DateOnly? ValidTo,
        string Street,
        string HouseNumber,
        string PostalCode,
        string City,
        string CountryCode,
        string Status);
}
