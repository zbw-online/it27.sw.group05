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
        decimal VatRate,
        string? Description,
        int Status);
}
