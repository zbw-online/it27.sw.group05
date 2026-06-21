namespace OrderManagement.Application.Features.Catalog.Shared
{
    public sealed record ArticleListItemDto(
        int ArticleId,
        string ArticleNumber,
        string Name,
        decimal PriceAmount,
        string PriceCurrency,
        int GroupId,
        string GroupName,
        int Stock,
        decimal VatRate,
        int Status);
}
