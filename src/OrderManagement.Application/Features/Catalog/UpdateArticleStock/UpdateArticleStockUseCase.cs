using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.UpdateArticleStock
{
    public sealed class UpdateArticleStockUseCase(
        IArticleCommandRepository articleCommandRepository,
        IUnitOfWork unitOfWork) : IUpdateArticleStockUseCase
    {
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            UpdateArticleStockCommand command,
            CancellationToken cancellationToken = default)
        {
            Article? article = await _articleCommandRepository.GetByIdAsync(
                new ArticleId(command.ArticleId),
                cancellationToken);

            if (article is null)
            {
                return Result.Fail("Article was not found.");
            }

            Result stockResult = article.UpdateStock(command.Delta);
            if (!stockResult.IsSuccess)
            {
                return stockResult;
            }

            _articleCommandRepository.Update(article);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
