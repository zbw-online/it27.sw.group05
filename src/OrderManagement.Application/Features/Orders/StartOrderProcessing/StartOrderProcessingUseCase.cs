using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Orders.Command;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.StartOrderProcessing
{
    public sealed class StartOrderProcessingUseCase(
        IOrderCommandRepository orderCommandRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : IStartOrderProcessingUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<Result> ExecuteAsync(
            StartOrderProcessingCommand command,
            CancellationToken cancellationToken = default)
        {
            Order? order = await _orderCommandRepository.GetByIdAsync(new OrderId(command.OrderId), cancellationToken);
            if (order is null)
            {
                return Result.Fail("Auftrag wurde nicht gefunden.");
            }

            Result transitionResult = order.StartProcessing(_timeProvider.GetUtcNow().UtcDateTime);
            if (!transitionResult.IsSuccess)
            {
                return transitionResult;
            }

            _orderCommandRepository.Update(order);
            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
