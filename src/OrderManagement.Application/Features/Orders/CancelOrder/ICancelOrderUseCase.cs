using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.CancelOrder
{
    public interface ICancelOrderUseCase
    {
        Task<Result> ExecuteAsync(
            CancelOrderCommand command,
            CancellationToken cancellationToken = default);
    }
}
