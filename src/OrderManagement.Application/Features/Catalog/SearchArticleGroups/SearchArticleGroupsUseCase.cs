using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Application.Features.Catalog.Shared;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.SearchArticleGroups
{
    public sealed class SearchArticleGroupsUseCase(
        IArticleGroupQueryRepository articleGroupQueryRepository) : ISearchArticleGroupsUseCase
    {
        private readonly IArticleGroupQueryRepository _articleGroupQueryRepository = articleGroupQueryRepository;

        public async Task<Result<IReadOnlyList<ArticleGroupListItemDto>>> ExecuteAsync(
            SearchArticleGroupsQuery query,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ArticleGroup> groups = query.ParentGroupId.HasValue
                ? await _articleGroupQueryRepository.GetByParentAsync(
                    new ArticleGroupId(query.ParentGroupId.Value),
                    cancellationToken)
                : await _articleGroupQueryRepository.GetListAsync(cancellationToken);
            string term = (query.SearchTerm ?? string.Empty).Trim();

            if (term.Length > 0)
            {
                groups = [.. groups.Where(g =>
                    g.Name.Contains(term, StringComparison.OrdinalIgnoreCase))];
            }

            var nameById = groups.ToDictionary(g => g.Id.Value, g => g.Name);

            IReadOnlyList<ArticleGroupListItemDto> result = [.. groups
                .OrderBy(g => g.Name)
                .Select(g => new ArticleGroupListItemDto(
                    g.Id.Value,
                    g.Name,
                    g.ParentGroupId?.Value,
                    g.ParentGroupId.HasValue && nameById.TryGetValue(g.ParentGroupId.Value.Value, out string? pName)
                        ? pName
                        : null))];

            return Results.Success(result);
        }
    }
}
