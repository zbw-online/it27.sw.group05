using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.DeleteArticle
{
    public interface IDeleteArticleUseCase
    {
        Task<Result> ExecuteAsync(
            DeleteArticleCommand command,
            CancellationToken cancellationToken = default);
    }
}
