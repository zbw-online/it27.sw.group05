using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.UpdateArticle
{
    public sealed class UpdateArticleUseCase(
        IArticleCommandRepository articleCommandRepository,
        IUnitOfWork unitOfWork) : IUpdateArticleUseCase
    {
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            UpdateArticleCommand command,
            CancellationToken cancellationToken = default)
        {
            Article? article = await _articleCommandRepository.GetByIdAsync(
                new ArticleId(command.ArticleId),
                cancellationToken);

            if (article is null)
            {
                return Result.Fail("Article was not found.");
            }

            Result<Money> moneyResult = Money.From(command.PriceAmount, command.PriceCurrency);
            if (!moneyResult.IsSuccess)
            {
                return Result.Fail(moneyResult.Error!);
            }

            Money newPrice = moneyResult.Value!;

            if (article.Price != newPrice)
            {
                Result priceResult = article.ChangePrice(newPrice);
                if (!priceResult.IsSuccess)
                {
                    return priceResult;
                }
            }

            var newGroupId = new ArticleGroupId(command.GroupId);
            if (article.ArticleGroupId != newGroupId)
            {
                Result groupResult = article.ChangeGroup(newGroupId);
                if (!groupResult.IsSuccess)
                {
                    return groupResult;
                }
            }

            int stockDelta = command.Stock - article.Stock;
            if (stockDelta != 0)
            {
                Result stockResult = article.UpdateStock(stockDelta);
                if (!stockResult.IsSuccess)
                {
                    return stockResult;
                }
            }

            if (command.ReorderPoint != article.ReorderPoint)
            {
                Result reorderPointResult = article.ChangeReorderPoint(command.ReorderPoint);
                if (!reorderPointResult.IsSuccess)
                {
                    return reorderPointResult;
                }
            }

            _articleCommandRepository.Update(article);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
