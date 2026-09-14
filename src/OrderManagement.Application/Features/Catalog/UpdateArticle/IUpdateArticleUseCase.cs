using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.UpdateArticle
{
    public interface IUpdateArticleUseCase
    {
        Task<Result> ExecuteAsync(
            UpdateArticleCommand command,
            CancellationToken cancellationToken = default);
    }
}
