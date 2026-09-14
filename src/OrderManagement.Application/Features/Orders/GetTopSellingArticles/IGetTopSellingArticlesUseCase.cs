using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetTopSellingArticles
{
    public interface IGetTopSellingArticlesUseCase
    {
        Task<Result<IReadOnlyList<TopSellingArticleDto>>> ExecuteAsync(
            GetTopSellingArticlesQuery query,
            CancellationToken cancellationToken = default);
    }
}
