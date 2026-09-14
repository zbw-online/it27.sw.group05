namespace OrderManagement.Application.Features.Orders.CreateOrder
{
    public sealed record CreateOrderCommand(
        string OrderNumber,
        int CustomerId,
        DateOnly DeliveryDate,
        string? CustomerReference,
        AddressOverrideInput? BillingAddressOverride,
        AddressOverrideInput? DeliveryAddressOverride,
        IReadOnlyList<CreateOrderLineInput> Lines);
}
