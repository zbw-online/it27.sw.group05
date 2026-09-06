using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Application.Abstractions.Persistence.Orders.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.DeleteOrder
{
    public sealed class DeleteOrderUseCase(
        IOrderCommandRepository orderCommandRepository,
        IArticleCommandRepository articleCommandRepository,
        IUnitOfWork unitOfWork) : IDeleteOrderUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            DeleteOrderCommand command,
            CancellationToken cancellationToken = default)
        {
            Order? order = await _orderCommandRepository.GetByIdAsync(new OrderId(command.OrderId), cancellationToken);
            if (order is null)
            {
                return Result.Fail("Auftrag wurde nicht gefunden.");
            }

            if (order.IsInventoryApplied)
            {
                Result restoreResult = await RestoreStockAsync(order, cancellationToken);
                if (!restoreResult.IsSuccess)
                {
                    return restoreResult;
                }
            }

            _orderCommandRepository.Remove(order);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }

        private async Task<Result> RestoreStockAsync(Order order, CancellationToken cancellationToken)
        {
            Dictionary<int, int> quantityByArticleId = [];
            foreach (OrderLine line in order.Lines)
            {
                quantityByArticleId[line.ArticleId.Value] =
                    quantityByArticleId.GetValueOrDefault(line.ArticleId.Value) + line.Quantity;
            }

            if (quantityByArticleId.Count == 0)
            {
                return Result.Success();
            }

            List<ArticleId> articleIds = [.. quantityByArticleId.Keys.Select(id => new ArticleId(id))];
            IReadOnlyList<Article> loadedArticles = await _articleCommandRepository.GetByIdsAsync(articleIds, cancellationToken);
            var articlesById = loadedArticles.ToDictionary(a => a.Id.Value);

            foreach (int articleId in quantityByArticleId.Keys)
            {
                if (!articlesById.ContainsKey(articleId))
                {
                    return Result.Fail($"Artikel mit ID {articleId} wurde nicht gefunden. Der Auftrag wurde nicht gelöscht.");
                }
            }

            foreach ((int articleId, int quantity) in quantityByArticleId)
            {
                Result stockResult = articlesById[articleId].UpdateStock(quantity);
                if (!stockResult.IsSuccess)
                {
                    return stockResult;
                }
            }

            foreach (Article article in articlesById.Values)
            {
                _articleCommandRepository.Update(article);
            }

            return Result.Success();
        }
    }
}
