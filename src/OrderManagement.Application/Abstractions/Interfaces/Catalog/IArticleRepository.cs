using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Interfaces.Catalog
{
    public interface IArticleRepository : IRepository<Article, ArticleId>
    {
        // Article-specific queries
        Task<Article?> GetByNumberAsync(
            ArticleNumber number,
            CancellationToken cancellationToken = default);


        // Business use cases
        Task<IReadOnlyList<Article>> GetByGroupAsync(
            ArticleGroupId groupId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Article>> GetLowStockAsync(
            int threshold,
            CancellationToken cancellationToken = default);
    }
}
