namespace OrderManagement.Application.Features.Customers.CreateCustomer
{
    public sealed record CreateCustomerResponse(
        int CustomerId,
        string CustomerNumber,
        string FullName,
        string Email);
}
