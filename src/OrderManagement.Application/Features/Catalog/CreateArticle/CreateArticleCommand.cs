namespace OrderManagement.Application.Features.Catalog.CreateArticle
{
    public sealed record CreateArticleCommand(
        string ArticleNumber,
        string Name,
        decimal PriceAmount,
        string PriceCurrency,
        int GroupId,
        int Stock,
        decimal VatRate,
        string? Description);
}
