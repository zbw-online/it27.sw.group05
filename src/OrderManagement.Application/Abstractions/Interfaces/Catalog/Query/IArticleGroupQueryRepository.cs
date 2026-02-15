using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Interfaces.Catalog.Query
{
    public interface IArticleGroupQueryRepository : IQueryRepository<ArticleGroup, ArticleGroupId>
    {
        Task<ArticleGroup?> GetByIdWithChildrenAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ArticleGroup>> GetByParentAsync(
            ArticleGroupId? parentId,
            CancellationToken cancellationToken = default);
    }
}
