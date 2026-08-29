namespace OrderManagement.Application.Features.Orders.CreateOrder
{
    public sealed record CreateOrderResponse(
        int OrderId,
        string OrderNumber,
        decimal TotalAmount,
        string TotalCurrency);
}
