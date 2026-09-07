using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.CompleteOrder
{
    public interface ICompleteOrderUseCase
    {
        Task<Result> ExecuteAsync(
            CompleteOrderCommand command,
            CancellationToken cancellationToken = default);
    }
}
