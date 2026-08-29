using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity
{
    public sealed class UpdateOrderLineQuantityUseCase(
        IOrderCommandRepository orderCommandRepository,
        IUnitOfWork unitOfWork) : IUpdateOrderLineQuantityUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            UpdateOrderLineQuantityCommand command,
            CancellationToken cancellationToken = default)
        {
            Order? order = await _orderCommandRepository.GetByIdAsync(new OrderId(command.OrderId), cancellationToken);
            if (order is null)
            {
                return Result.Fail("Order was not found.");
            }

            Result updateResult = order.UpdateLineQuantity(new OrderLineId(command.OrderLineId), command.Quantity);
            if (!updateResult.IsSuccess)
            {
                return updateResult;
            }

            _orderCommandRepository.Update(order);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
