namespace OrderManagement.Application.Features.Orders.Shared
{
    public sealed record OrderDraftTotalsDto(
        decimal Subtotal,
        decimal VatAmount,
        decimal Total,
        string Currency);
}
