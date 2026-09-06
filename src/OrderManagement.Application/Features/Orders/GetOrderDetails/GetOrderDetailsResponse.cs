using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Application.Features.Orders.GetOrderDetails
{
    public sealed record GetOrderDetailsResponse(
        int OrderId,
        string OrderNumber,
        DateTime OrderDate,
        DateOnly DeliveryDate,
        string? CustomerReference,
        int CustomerId,
        string CustomerNumber,
        string CustomerName,
        string BillingStreet,
        string BillingHouseNumber,
        string BillingPostalCode,
        string BillingCity,
        string BillingCountryCode,
        AddressSource BillingAddressSource,
        string DeliveryStreet,
        string DeliveryHouseNumber,
        string DeliveryPostalCode,
        string DeliveryCity,
        string DeliveryCountryCode,
        AddressSource DeliveryAddressSource,
        decimal TotalAmount,
        string TotalCurrency,
        IReadOnlyList<OrderLineDto> Lines);
}
