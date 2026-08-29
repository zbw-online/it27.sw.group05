namespace OrderManagement.Application.Features.Orders.Shared
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
