namespace OrderManagement.Application.Features.Orders.Shared
{
    public sealed record OrderDraftLineInput(
        decimal UnitPriceAmount,
        string Currency,
        int Quantity,
        decimal VatRate);
}
