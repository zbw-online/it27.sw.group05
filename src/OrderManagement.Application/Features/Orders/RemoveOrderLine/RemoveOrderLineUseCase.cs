using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.RemoveOrderLine
{
    public sealed class RemoveOrderLineUseCase(
        IOrderCommandRepository orderCommandRepository,
        IArticleCommandRepository articleCommandRepository,
        IUnitOfWork unitOfWork) : IRemoveOrderLineUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            RemoveOrderLineCommand command,
            CancellationToken cancellationToken = default)
        {
            Order? order = await _orderCommandRepository.GetByIdAsync(new OrderId(command.OrderId), cancellationToken);
            if (order is null)
            {
                return Result.Fail("Order was not found.");
            }

            var lineId = new OrderLineId(command.OrderLineId);
            OrderLine? removedLine = order.Lines.FirstOrDefault(l => l.Id == lineId);

            Result removeResult = order.RemoveLine(lineId);
            if (!removeResult.IsSuccess)
            {
                return removeResult;
            }

            if (removedLine is not null)
            {
                Article? article = await _articleCommandRepository.GetByIdAsync(removedLine.ArticleId, cancellationToken);
                if (article is not null)
                {
                    _ = article.UpdateStock(removedLine.Quantity);
                    _articleCommandRepository.Update(article);
                }
            }

            _orderCommandRepository.Update(order);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
