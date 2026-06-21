using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Application.DTOs.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.GetArticleGroupHierarchy
{
    public sealed class GetArticleGroupHierarchyUseCase(
        IArticleGroupQueryRepository articleGroupQueryRepository) : IGetArticleGroupHierarchyUseCase
    {
        private readonly IArticleGroupQueryRepository _articleGroupQueryRepository = articleGroupQueryRepository;

        public async Task<Result<IReadOnlyList<ArticleGroupHierarchyDto>>> ExecuteAsync(
            GetArticleGroupHierarchyQuery query,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ArticleGroupHierarchyDto> hierarchy;

            if (query.RootGroupId.HasValue)
            {
                hierarchy = await _articleGroupQueryRepository.GetHierarchyFromRootAsync(
                    new ArticleGroupId(query.RootGroupId.Value),
                    cancellationToken);
            }
            else
            {
                hierarchy = await _articleGroupQueryRepository.GetFullHierarchyAsync(cancellationToken);
            }

            return Results.Success(hierarchy);
        }
    }
}
