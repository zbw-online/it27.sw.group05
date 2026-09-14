using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity
{
    public interface IUpdateOrderLineQuantityUseCase
    {
        Task<Result> ExecuteAsync(
            UpdateOrderLineQuantityCommand command,
            CancellationToken cancellationToken = default);
    }
}
