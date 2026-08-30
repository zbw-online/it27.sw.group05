namespace OrderManagement.Application.Features.Customers.GetCustomersWithoutCurrentAddress
{
    public sealed record CustomerWithoutAddressDto(
        int CustomerId,
        string CustomerNumber,
        string FullName);
}
