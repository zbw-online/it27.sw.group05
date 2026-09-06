using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Application.Features.Catalog.Contracts
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
        int ReorderPoint,
        StockLevel StockLevel,
        decimal VatRate,
        ArticleStatus Status);
}
