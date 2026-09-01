namespace OrderManagement.Application.Features.Catalog.UpdateArticle
{
    public sealed record UpdateArticleCommand(
        int ArticleId,
        string Name,
        decimal PriceAmount,
        string PriceCurrency,
        int GroupId,
        int Stock,
        int ReorderPoint,
        decimal VatRate,
        string? Description);
}
