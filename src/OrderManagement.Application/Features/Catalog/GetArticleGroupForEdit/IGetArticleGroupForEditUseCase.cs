using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.GetArticleGroupForEdit
{
    public interface IGetArticleGroupForEditUseCase
    {
        Task<Result<GetArticleGroupForEditResponse>> ExecuteAsync(
            GetArticleGroupForEditQuery query,
            CancellationToken cancellationToken = default);
    }
}
