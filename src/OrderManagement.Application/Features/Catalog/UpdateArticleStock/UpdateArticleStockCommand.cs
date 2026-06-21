namespace OrderManagement.Application.Features.Catalog.UpdateArticleStock
{
    public sealed record UpdateArticleStockCommand(int ArticleId, int Delta);
}
