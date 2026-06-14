namespace OrderManagement.Application.Features.Customers.GetCustomerDetails
{
    public sealed record GetCustomerDetailsResponse(
        int CustomerId,
        string CustomerNumber,
        string FullName,
        string Email,
        string? Website,
        CustomerAddressDto? CurrentAddress,
        IReadOnlyList<CustomerAddressDto> PreviousAddresses,
        IReadOnlyList<CustomerAddressDto> FutureAddresses);
}
