using OrderManagement.Application.Features.Catalog.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.GetArticleGroupHierarchy
{
    public interface IGetArticleGroupHierarchyUseCase
    {
        Task<Result<IReadOnlyList<ArticleGroupHierarchyDto>>> ExecuteAsync(
            GetArticleGroupHierarchyQuery query,
            CancellationToken cancellationToken = default);
    }
}
