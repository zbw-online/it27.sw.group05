namespace OrderManagement.Application.Features.Customers.DataExchange.Contracts
{
    public sealed record CustomerAddressDataDto(
        DateOnly ValidFrom,
        string Street,
        string HouseNumber,
        string PostalCode,
        string City,
        string CountryCode);
}
