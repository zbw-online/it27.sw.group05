namespace OrderManagement.Application.Features.Customers.PreviewAddressForDate
{
    public sealed record PreviewAddressForDateResponse(
        bool HasValidAddress,
        string? Street,
        string? HouseNumber,
        string? PostalCode,
        string? City,
        string? CountryCode,
        DateOnly? ValidFrom,
        DateOnly? ValidTo);
}
