namespace OrderManagement.Application.Features.Catalog.CreateArticle
{
    public sealed record CreateArticleResponse(
        int ArticleId,
        string ArticleNumber,
        string Name,
        decimal PriceAmount,
        string PriceCurrency);
}
