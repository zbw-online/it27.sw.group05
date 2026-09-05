namespace OrderManagement.Application.Features.Customers.PreviewAddressForDate
{
    public sealed record PreviewAddressForDateQuery(int CustomerId, DateOnly Date);
}
