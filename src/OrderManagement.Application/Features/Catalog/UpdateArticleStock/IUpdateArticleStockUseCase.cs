using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.UpdateArticleStock
{
    public interface IUpdateArticleStockUseCase
    {
        Task<Result> ExecuteAsync(
            UpdateArticleStockCommand command,
            CancellationToken cancellationToken = default);
    }
}
