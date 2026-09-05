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
            IReadOnlyList<ArticleGroup> groups = await _articleGroupQueryRepository.GetListAsync(cancellationToken);

            List<ArticleGroupId>? groupIds = null;
            if (query.GroupId.HasValue)
            {
                HashSet<int> descendantIds = CollectSelfAndDescendantIds(groups, query.GroupId.Value);
                groupIds = [.. descendantIds.Select(id => new ArticleGroupId(id))];
            }

            IReadOnlyList<Article> articles = await _articleQueryRepository.SearchAsync(
                groupIds, query.StatusFilter, query.SearchTerm, cancellationToken);

            var groupNames = groups.ToDictionary(g => g.Id.Value, g => g.Name);

            IReadOnlyList<ArticleListItemDto> result = [.. articles
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

        private static HashSet<int> CollectSelfAndDescendantIds(IReadOnlyList<ArticleGroup> groups, int rootId)
        {
            var result = new HashSet<int> { rootId };
            var pending = new Queue<int>();
            pending.Enqueue(rootId);

            while (pending.Count > 0)
            {
                int currentId = pending.Dequeue();
                foreach (ArticleGroup child in groups.Where(g => g.ParentGroupId?.Value == currentId))
                {
                    if (result.Add(child.Id.Value))
                    {
                        pending.Enqueue(child.Id.Value);
                    }
                }
            }

            return result;
        }
    }
}
