namespace OrderManagement.Application.Features.Orders.GetTopSellingArticles
{
    public sealed record TopSellingArticleDto(
        int ArticleId,
        string ArticleNumber,
        string ArticleName,
        int TotalQuantity,
        int OrderCount);
}
