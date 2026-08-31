namespace OrderManagement.Application.Features.Catalog.ReconcileInventory
{
    public sealed record ReconciliationReportDto(
        IReadOnlyList<string> AffectedOrderNumbers,
        IReadOnlyList<ReconciliationArticleImpactDto> ArticleImpacts,
        IReadOnlyList<string> Conflicts,
        bool WasApplied);
}
