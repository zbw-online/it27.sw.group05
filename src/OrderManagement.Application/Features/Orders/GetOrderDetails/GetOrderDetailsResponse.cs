using OrderManagement.Application.Features.Orders.Shared;

namespace OrderManagement.Application.Features.Orders.GetOrderDetails
{
    public sealed record GetOrderDetailsResponse(
        int OrderId,
        string OrderNumber,
        DateTime OrderDate,
        int CustomerId,
        string CustomerNumber,
        string CustomerName,
        string DeliveryStreet,
        string DeliveryHouseNumber,
        string DeliveryPostalCode,
        string DeliveryCity,
        string DeliveryCountryCode,
        decimal TotalAmount,
        string TotalCurrency,
        IReadOnlyList<OrderLineDto> Lines);
}
