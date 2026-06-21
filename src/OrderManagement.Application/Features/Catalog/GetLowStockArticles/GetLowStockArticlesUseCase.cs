using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Application.Features.Catalog.Shared;
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
            if (query.Threshold < 0)
            {
                return Results.Fail<IReadOnlyList<ArticleListItemDto>>("Threshold cannot be negative.");
            }

            IReadOnlyList<Article> articles = await _articleQueryRepository.GetLowStockAsync(
                query.Threshold,
                cancellationToken);

            IReadOnlyList<ArticleGroup> groups = await _articleGroupQueryRepository.GetListAsync(cancellationToken);
            Dictionary<int, string> groupNames = groups.ToDictionary(g => g.Id.Value, g => g.Name);

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
                    a.VatRate,
                    a.Status))];

            return Results.Success(result);
        }
    }
}
