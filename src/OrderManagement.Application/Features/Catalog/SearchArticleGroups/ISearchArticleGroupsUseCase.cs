using OrderManagement.Application.Features.Catalog.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.SearchArticleGroups
{
    public interface ISearchArticleGroupsUseCase
    {
        Task<Result<IReadOnlyList<ArticleGroupListItemDto>>> ExecuteAsync(
            SearchArticleGroupsQuery query,
            CancellationToken cancellationToken = default);
    }
}
