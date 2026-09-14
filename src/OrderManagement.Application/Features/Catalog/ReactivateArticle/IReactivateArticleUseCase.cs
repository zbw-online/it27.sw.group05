using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.ReactivateArticle
{
    public interface IReactivateArticleUseCase
    {
        Task<Result> ExecuteAsync(
            ReactivateArticleCommand command,
            CancellationToken cancellationToken = default);
    }
}
