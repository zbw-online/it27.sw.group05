using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Application.Features.Catalog.GetArticleForEdit
{
    public sealed record GetArticleForEditResponse(
        int ArticleId,
        string ArticleNumber,
        string Name,
        decimal PriceAmount,
        string PriceCurrency,
        int GroupId,
        int Stock,
        int ReorderPoint,
        decimal VatRate,
        string? Description,
        ArticleStatus Status);
}
