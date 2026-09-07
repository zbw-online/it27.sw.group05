using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Orders.Command;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.CompleteOrder
{
    public sealed class CompleteOrderUseCase(
        IOrderCommandRepository orderCommandRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : ICompleteOrderUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<Result> ExecuteAsync(
            CompleteOrderCommand command,
            CancellationToken cancellationToken = default)
        {
            Order? order = await _orderCommandRepository.GetByIdAsync(new OrderId(command.OrderId), cancellationToken);
            if (order is null)
            {
                return Result.Fail("Auftrag wurde nicht gefunden.");
            }

            Result transitionResult = order.Complete(_timeProvider.GetUtcNow().UtcDateTime);
            if (!transitionResult.IsSuccess)
            {
                return transitionResult;
            }

            _orderCommandRepository.Update(order);
            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
