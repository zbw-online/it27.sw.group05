namespace OrderManagement.Application.Features.Orders.Contracts
{
    public sealed record OrderDraftLineInput(
        decimal UnitPriceAmount,
        string Currency,
        int Quantity,
        decimal VatRate);
}
