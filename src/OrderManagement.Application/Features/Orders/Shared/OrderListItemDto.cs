namespace OrderManagement.Application.Features.Orders.Shared
{
    public sealed record OrderListItemDto(
        int OrderId,
        string OrderNumber,
        DateTime OrderDate,
        int CustomerId,
        string CustomerNumber,
        int LineCount,
        decimal TotalAmount,
        string TotalCurrency);
}
