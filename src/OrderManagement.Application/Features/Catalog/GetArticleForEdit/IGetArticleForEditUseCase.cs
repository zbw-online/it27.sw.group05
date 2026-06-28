using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.GetArticleForEdit
{
    public interface IGetArticleForEditUseCase
    {
        Task<Result<GetArticleForEditResponse>> ExecuteAsync(
            GetArticleForEditQuery query,
            CancellationToken cancellationToken = default);
    }
}
