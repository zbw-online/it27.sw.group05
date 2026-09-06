namespace OrderManagement.Application.Features.Orders.Contracts
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
