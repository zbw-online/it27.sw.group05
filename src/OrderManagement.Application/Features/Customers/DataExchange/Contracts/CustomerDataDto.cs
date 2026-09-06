namespace OrderManagement.Application.Features.Customers.DataExchange.Contracts
{
    public sealed record CustomerDataDto(
        string CustomerNumber,
        string LastName,
        string SurName,
        string Email,
        string? Website,
        CustomerAddressDataDto? Address);
}
