namespace OrderManagement.Application.Features.Orders.Contracts
{
    public sealed record OrderDraftTotalsDto(
        decimal Subtotal,
        decimal VatAmount,
        decimal Total,
        string Currency);
}
