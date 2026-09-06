using OrderManagement.Application.Features.Catalog.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.GetLowStockArticles
{
    public interface IGetLowStockArticlesUseCase
    {
        Task<Result<IReadOnlyList<ArticleListItemDto>>> ExecuteAsync(
            GetLowStockArticlesQuery query,
            CancellationToken cancellationToken = default);
    }
}
