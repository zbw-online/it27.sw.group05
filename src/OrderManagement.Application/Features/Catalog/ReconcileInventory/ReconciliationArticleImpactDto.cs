namespace OrderManagement.Application.Features.Catalog.ReconcileInventory
{
    public sealed record ReconciliationArticleImpactDto(
        int ArticleId,
        string ArticleNumber,
        int CurrentStock,
        int QuantityToDeduct,
        int ResultingStock,
        bool HasInsufficientStock);
}
