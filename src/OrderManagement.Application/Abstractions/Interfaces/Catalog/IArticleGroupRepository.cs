using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Interfaces.Catalog
{
    public interface IArticleGroupRepository : IRepository<ArticleGroup, ArticleGroupId>
    {
        // ArticleGroup-specific queries  
        Task<ArticleGroup?> GetByIdWithChildrenAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default);

        Task<ArticleGroup?> GetByIdWithArticlesAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default);

        // Hierarchy navigation
        Task<IReadOnlyList<ArticleGroup>> GetByParentAsync(
            ArticleGroupId? parentId,
            CancellationToken cancellationToken = default);
    }
}
