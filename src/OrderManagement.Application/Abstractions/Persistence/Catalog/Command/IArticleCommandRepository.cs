using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Persistence.Catalog.Command
{
    public interface IArticleCommandRepository : ICommandRepository<Article, ArticleId>
    {
        Task<Article?> GetByIdAsync(
            ArticleId id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Article>> GetByIdsAsync(
            IReadOnlyCollection<ArticleId> ids,
            CancellationToken cancellationToken = default);
    }
}
