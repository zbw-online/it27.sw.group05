using OrderManagement.Application.Abstractions.Persistence.Catalog.Query;
using OrderManagement.Application.Features.Catalog.Contracts;
using OrderManagement.Domain.Catalog;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.GetLowStockArticles
{
    public sealed class GetLowStockArticlesUseCase(
        IArticleQueryRepository articleQueryRepository,
        IArticleGroupQueryRepository articleGroupQueryRepository) : IGetLowStockArticlesUseCase
    {
        private readonly IArticleQueryRepository _articleQueryRepository = articleQueryRepository;
        private readonly IArticleGroupQueryRepository _articleGroupQueryRepository = articleGroupQueryRepository;

        public async Task<Result<IReadOnlyList<ArticleListItemDto>>> ExecuteAsync(
            GetLowStockArticlesQuery query,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Article> articles = await _articleQueryRepository.GetLowStockAsync(cancellationToken);

            IReadOnlyList<ArticleGroup> groups = await _articleGroupQueryRepository.GetListAsync(cancellationToken);
            var groupNames = groups.ToDictionary(g => g.Id.Value, g => g.Name);

            IReadOnlyList<ArticleListItemDto> result = [.. articles
                .OrderBy(a => a.Stock)
                .ThenBy(a => a.ArticleNumber.Value)
                .Select(a => new ArticleListItemDto(
                    a.Id.Value,
                    a.ArticleNumber.Value,
                    a.Name,
                    a.Price.Amount,
                    a.Price.Currency,
                    a.ArticleGroupId.Value,
                    groupNames.TryGetValue(a.ArticleGroupId.Value, out string? gName) ? gName : string.Empty,
                    a.Stock,
                    a.ReorderPoint,
                    a.StockLevel,
                    a.VatRate,
                    a.Status))];

            return Results.Success(result);
        }
    }
}
