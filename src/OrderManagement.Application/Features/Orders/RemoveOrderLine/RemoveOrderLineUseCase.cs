using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.RemoveOrderLine
{
    public sealed class RemoveOrderLineUseCase(
        IOrderCommandRepository orderCommandRepository,
        IUnitOfWork unitOfWork) : IRemoveOrderLineUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
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

            Result removeResult = order.RemoveLine(new OrderLineId(command.OrderLineId));
            if (!removeResult.IsSuccess)
            {
                return removeResult;
            }

            _orderCommandRepository.Update(order);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
