using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.CreateArticleGroup
{
    public interface ICreateArticleGroupUseCase
    {
        Task<Result<CreateArticleGroupResponse>> ExecuteAsync(
            CreateArticleGroupCommand command,
            CancellationToken cancellationToken = default);
    }
}
