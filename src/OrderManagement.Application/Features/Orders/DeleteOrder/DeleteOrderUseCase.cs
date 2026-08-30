using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Domain.Catalog;
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
                return Result.Fail("Order was not found.");
            }

            foreach (OrderLine line in order.Lines)
            {
                Article? article = await _articleCommandRepository.GetByIdAsync(line.ArticleId, cancellationToken);
                if (article is not null)
                {
                    _ = article.UpdateStock(line.Quantity);
                    _articleCommandRepository.Update(article);
                }
            }

            _orderCommandRepository.Remove(order);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
