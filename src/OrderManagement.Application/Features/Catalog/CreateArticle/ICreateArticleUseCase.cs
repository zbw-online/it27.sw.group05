using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.CreateArticle
{
    public interface ICreateArticleUseCase
    {
        Task<Result<CreateArticleResponse>> ExecuteAsync(
            CreateArticleCommand command,
            CancellationToken cancellationToken = default);
    }
}
