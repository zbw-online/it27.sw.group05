using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.ReconcileInventory
{
    public sealed class ReconcileInventoryUseCase(
        IOrderQueryRepository orderQueryRepository,
        IOrderCommandRepository orderCommandRepository,
        IArticleCommandRepository articleCommandRepository,
        IUnitOfWork unitOfWork) : IReconcileInventoryUseCase
    {
        private readonly IOrderQueryRepository _orderQueryRepository = orderQueryRepository;
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<ReconciliationReportDto>> ExecuteAsync(
            ReconcileInventoryCommand command,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Order> unreconciledOrders = await _orderQueryRepository.GetUnreconciledOrdersAsync(cancellationToken);

            if (unreconciledOrders.Count == 0)
            {
                return Results.Success(new ReconciliationReportDto([], [], [], false));
            }

            Dictionary<int, int> quantityByArticleId = [];
            foreach (Order order in unreconciledOrders)
            {
                foreach (OrderLine line in order.Lines)
                {
                    quantityByArticleId[line.ArticleId.Value] =
                        quantityByArticleId.GetValueOrDefault(line.ArticleId.Value) + line.Quantity;
                }
            }

            List<ArticleId> articleIds = [.. quantityByArticleId.Keys.Select(id => new ArticleId(id))];
            IReadOnlyList<Article> loadedArticles = await _articleCommandRepository.GetByIdsAsync(articleIds, cancellationToken);
            var articlesById = loadedArticles.ToDictionary(a => a.Id.Value);

            var impacts = new List<ReconciliationArticleImpactDto>();
            var conflicts = new List<string>();

            foreach ((int articleId, int totalQuantity) in quantityByArticleId)
            {
                if (!articlesById.TryGetValue(articleId, out Article? article))
                {
                    conflicts.Add($"Artikel mit ID {articleId} wurde nicht gefunden.");
                    continue;
                }

                int resultingStock = article.Stock - totalQuantity;
                bool insufficient = resultingStock < 0;
                if (insufficient)
                {
                    conflicts.Add(
                        $"Artikel '{article.ArticleNumber.Value}' hat nicht genügend Lagerbestand für den Ausgleich (verfügbar: {article.Stock}, benötigt: {totalQuantity}).");
                }

                impacts.Add(new ReconciliationArticleImpactDto(
                    article.Id.Value,
                    article.ArticleNumber.Value,
                    article.Stock,
                    totalQuantity,
                    resultingStock,
                    insufficient));
            }

            string[] orderNumbers = [.. unreconciledOrders.Select(o => o.OrderNumber.Value)];

            if (!command.Apply || conflicts.Count > 0)
            {
                return Results.Success(new ReconciliationReportDto(orderNumbers, impacts, conflicts, false));
            }

            foreach ((int articleId, int totalQuantity) in quantityByArticleId)
            {
                Article article = articlesById[articleId];
                Result stockResult = article.UpdateStock(-totalQuantity);
                if (!stockResult.IsSuccess)
                {
                    return Results.Fail<ReconciliationReportDto>(stockResult.Error!);
                }

                _articleCommandRepository.Update(article);
            }

            foreach (Order order in unreconciledOrders)
            {
                Result markResult = order.MarkInventoryApplied();
                if (!markResult.IsSuccess)
                {
                    return Results.Fail<ReconciliationReportDto>(markResult.Error!);
                }

                _orderCommandRepository.Update(order);
            }

            Result commitResult = await _unitOfWork.CommitAsync(cancellationToken);
            return !commitResult.IsSuccess
                ? Results.Fail<ReconciliationReportDto>(commitResult.Error!)
                : Results.Success(new ReconciliationReportDto(orderNumbers, impacts, conflicts, true));
        }
    }
}
