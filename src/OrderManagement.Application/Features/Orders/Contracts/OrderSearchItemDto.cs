using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Application.Features.Orders.Contracts
{
    public sealed record OrderSearchItemDto(
        int OrderId,
        string OrderNumber,
        DateTime OrderDate,
        DateOnly DeliveryDate,
        int CustomerId,
        string CustomerNumber,
        int LineCount,
        decimal TotalAmount,
        string TotalCurrency,
        OrderStatus Status,
        bool IsOverdue);
}
