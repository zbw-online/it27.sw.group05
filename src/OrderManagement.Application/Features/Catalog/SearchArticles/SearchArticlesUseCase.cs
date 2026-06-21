using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Application.Features.Catalog.Shared;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.SearchArticles
{
    public sealed class SearchArticlesUseCase(
        IArticleQueryRepository articleQueryRepository,
        IArticleGroupQueryRepository articleGroupQueryRepository) : ISearchArticlesUseCase
    {
        private readonly IArticleQueryRepository _articleQueryRepository = articleQueryRepository;
        private readonly IArticleGroupQueryRepository _articleGroupQueryRepository = articleGroupQueryRepository;

        public async Task<Result<IReadOnlyList<ArticleListItemDto>>> ExecuteAsync(
            SearchArticlesQuery query,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Article> articles = query.GroupId.HasValue
                ? await _articleQueryRepository.GetByGroupAsync(
                    new ArticleGroupId(query.GroupId.Value),
                    cancellationToken)
                : await _articleQueryRepository.GetListAsync(cancellationToken);
            string term = (query.SearchTerm ?? string.Empty).Trim().ToUpperInvariant();

            if (term.Length > 0)
            {
                articles = [.. articles.Where(a =>
                    a.ArticleNumber.Value.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    a.Name.Contains(term, StringComparison.OrdinalIgnoreCase))];
            }

            IReadOnlyList<ArticleGroup> groups = await _articleGroupQueryRepository.GetListAsync(cancellationToken);
            var groupNames = groups.ToDictionary(g => g.Id.Value, g => g.Name);

            IReadOnlyList<ArticleListItemDto> result = [.. articles
                .OrderBy(a => a.ArticleNumber.Value)
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
