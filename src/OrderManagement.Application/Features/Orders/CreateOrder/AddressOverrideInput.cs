namespace OrderManagement.Application.Features.Orders.CreateOrder
{
    public sealed record AddressOverrideInput(
        string Street,
        string HouseNumber,
        string PostalCode,
        string City,
        string CountryCode);
}
