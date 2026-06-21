using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.GetArticleGroupForEdit
{
    public sealed class GetArticleGroupForEditUseCase(
        IArticleGroupQueryRepository articleGroupQueryRepository) : IGetArticleGroupForEditUseCase
    {
        private readonly IArticleGroupQueryRepository _articleGroupQueryRepository = articleGroupQueryRepository;

        public async Task<Result<GetArticleGroupForEditResponse>> ExecuteAsync(
            GetArticleGroupForEditQuery query,
            CancellationToken cancellationToken = default)
        {
            ArticleGroup? group = await _articleGroupQueryRepository.GetByIdAsync(
                new ArticleGroupId(query.ArticleGroupId),
                cancellationToken);

            if (group is null)
            {
                return Results.Fail<GetArticleGroupForEditResponse>("Article group was not found.");
            }

            return Results.Success(new GetArticleGroupForEditResponse(
                group.Id.Value,
                group.Name,
                group.ParentGroupId?.Value));
        }
    }
}
