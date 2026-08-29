using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.AddOrderLine
{
    public sealed class AddOrderLineUseCase(
        IOrderCommandRepository orderCommandRepository,
        IArticleQueryRepository articleQueryRepository,
        IUnitOfWork unitOfWork) : IAddOrderLineUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IArticleQueryRepository _articleQueryRepository = articleQueryRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            AddOrderLineCommand command,
            CancellationToken cancellationToken = default)
        {
            Order? order = await _orderCommandRepository.GetByIdAsync(new OrderId(command.OrderId), cancellationToken);
            if (order is null)
            {
                return Result.Fail("Order was not found.");
            }

            Article? article = await _articleQueryRepository.GetByIdAsync(
                new ArticleId(command.ArticleId),
                cancellationToken);

            if (article is null)
            {
                return Result.Fail("Article was not found.");
            }

            Result addLineResult = order.AddLine(article.Id, article.Name, article.Price, command.Quantity);
            if (!addLineResult.IsSuccess)
            {
                return addLineResult;
            }

            _orderCommandRepository.Update(order);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
