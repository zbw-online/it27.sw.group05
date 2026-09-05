namespace OrderManagement.Application.Features.Customers.DataExchange.Shared
{
    public sealed record CustomerDataDto(
        string CustomerNumber,
        string LastName,
        string SurName,
        string Email,
        string? Website,
        CustomerAddressDataDto? Address);
}
