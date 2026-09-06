using OrderManagement.Application.Features.Catalog.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.SearchArticles
{
    public interface ISearchArticlesUseCase
    {
        Task<Result<IReadOnlyList<ArticleListItemDto>>> ExecuteAsync(
            SearchArticlesQuery query,
            CancellationToken cancellationToken = default);
    }
}
