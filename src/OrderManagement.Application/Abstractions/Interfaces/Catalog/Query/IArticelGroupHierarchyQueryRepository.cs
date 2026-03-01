using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Application.Abstractions.Interfaces.Catalog.Query
{
    public interface IArticleGroupHierarchyQueryRepository
    {
        Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetHierarchyFromRootAsync(
            ArticleGroupId rootId,
            CancellationToken ct = default);

        Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetFullHierarchyAsync(
            CancellationToken ct = default);
    }

    public sealed record ArticleGroupHierarchyDto(
        ArticleGroupId Id,
        string Name,
        ArticleGroupId? ParentGroupId,
        int Level,
        string Path);
}
