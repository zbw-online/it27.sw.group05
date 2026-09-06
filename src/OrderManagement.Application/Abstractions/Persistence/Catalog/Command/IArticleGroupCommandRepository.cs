using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Persistence.Catalog.Command
{
    public interface IArticleGroupCommandRepository : ICommandRepository<ArticleGroup, ArticleGroupId>
    {
        Task<ArticleGroup?> GetByIdAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default);

        Task<ArticleGroup?> GetByIdWithChildrenAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default);
    }
}
