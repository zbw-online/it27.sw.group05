using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.RenameArticleGroup
{
    public interface IRenameArticleGroupUseCase
    {
        Task<Result> ExecuteAsync(
            RenameArticleGroupCommand command,
            CancellationToken cancellationToken = default);
    }
}
