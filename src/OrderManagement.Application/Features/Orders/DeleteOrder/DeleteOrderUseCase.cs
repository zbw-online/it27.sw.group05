using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.DeleteOrder
{
    public sealed class DeleteOrderUseCase(
        IOrderCommandRepository orderCommandRepository,
        IUnitOfWork unitOfWork) : IDeleteOrderUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
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

            _orderCommandRepository.Remove(order);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
