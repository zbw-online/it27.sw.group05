using OrderManagement.Application.Features.Catalog.Contracts;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Persistence.Catalog.Query
{
    public interface IArticleGroupQueryRepository : IQueryRepository<ArticleGroup, ArticleGroupId>
    {
        Task<ArticleGroup?> GetByIdWithChildrenAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ArticleGroup>> GetByParentAsync(
            ArticleGroupId? parentId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetHierarchyFromRootAsync(
            ArticleGroupId rootId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetFullHierarchyAsync(
            CancellationToken cancellationToken = default);
    }
}
