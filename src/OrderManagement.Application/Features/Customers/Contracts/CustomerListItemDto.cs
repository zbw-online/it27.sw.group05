namespace OrderManagement.Application.Features.Customers.Contracts
{
    public sealed record CustomerListItemDto(
        int CustomerId,
        string CustomerNumber,
        string FullName,
        string Email,
        string? Website,
        string Street,
        string HouseNumber,
        string PostalCode,
        string City,
        string CountryCode);
}
