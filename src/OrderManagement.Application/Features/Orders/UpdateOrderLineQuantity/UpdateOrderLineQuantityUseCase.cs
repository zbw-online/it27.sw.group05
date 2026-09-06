using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Application.Abstractions.Persistence.Orders.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity
{
    public sealed class UpdateOrderLineQuantityUseCase(
        IOrderCommandRepository orderCommandRepository,
        IArticleCommandRepository articleCommandRepository,
        IUnitOfWork unitOfWork) : IUpdateOrderLineQuantityUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            UpdateOrderLineQuantityCommand command,
            CancellationToken cancellationToken = default)
        {
            Order? order = await _orderCommandRepository.GetByIdAsync(new OrderId(command.OrderId), cancellationToken);
            if (order is null)
            {
                return Result.Fail("Auftrag wurde nicht gefunden.");
            }

            var lineId = new OrderLineId(command.OrderLineId);
            OrderLine? line = order.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null)
            {
                return Result.Fail("Auftragsposition wurde nicht gefunden.");
            }

            int previousQuantity = line.Quantity;

            Result updateResult = order.UpdateLineQuantity(lineId, command.Quantity);
            if (!updateResult.IsSuccess)
            {
                return updateResult;
            }

            Article? article = await _articleCommandRepository.GetByIdAsync(line.ArticleId, cancellationToken);
            if (article is not null)
            {
                Result stockResult = article.UpdateStock(previousQuantity - command.Quantity);
                if (!stockResult.IsSuccess)
                {
                    return stockResult;
                }

                _articleCommandRepository.Update(article);
            }

            _orderCommandRepository.Update(order);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
