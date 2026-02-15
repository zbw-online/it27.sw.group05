using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Interfaces.Catalog.Query
{
    public interface IArticleQueryRepository : IQueryRepository<Article, ArticleId>
    {
        Task<Article?> GetByNumberAsync(
            ArticleNumber number,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Article>> GetByGroupAsync(
            ArticleGroupId groupId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Article>> GetLowStockAsync(
            int threshold,
            CancellationToken cancellationToken = default);
    }
}
