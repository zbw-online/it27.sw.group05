namespace OrderManagement.Application.Features.Orders.Contracts
{
    public sealed record OrderLineDto(
        int OrderLineId,
        int LineNumber,
        int ArticleId,
        string ArticleName,
        decimal UnitPriceAmount,
        string UnitPriceCurrency,
        int Quantity,
        decimal LineTotalAmount,
        string LineTotalCurrency);
}
