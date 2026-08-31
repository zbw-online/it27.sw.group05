using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.DeactivateArticle
{
    public interface IDeactivateArticleUseCase
    {
        Task<Result> ExecuteAsync(
            DeactivateArticleCommand command,
            CancellationToken cancellationToken = default);
    }
}
