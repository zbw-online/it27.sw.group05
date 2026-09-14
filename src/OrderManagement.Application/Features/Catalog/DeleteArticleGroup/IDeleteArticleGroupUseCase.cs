using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.DeleteArticleGroup
{
    public interface IDeleteArticleGroupUseCase
    {
        Task<Result> ExecuteAsync(
            DeleteArticleGroupCommand command,
            CancellationToken cancellationToken = default);
    }
}
