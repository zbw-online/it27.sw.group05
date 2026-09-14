namespace OrderManagement.Application.Features.Customers.AddCustomerAddress
{
    public sealed record AddCustomerAddressCommand(
        int CustomerId,
        DateOnly ValidFrom,
        string Street,
        string HouseNumber,
        string PostalCode,
        string City,
        string CountryCode);
}
