namespace OrderManagement.Application.Features.Orders.CreateOrder
{
    public sealed record CreateOrderCommand(
        string OrderNumber,
        int CustomerId,
        string Street,
        string HouseNumber,
        string PostalCode,
        string City,
        string CountryCode,
        IReadOnlyList<CreateOrderLineInput> Lines);
}
