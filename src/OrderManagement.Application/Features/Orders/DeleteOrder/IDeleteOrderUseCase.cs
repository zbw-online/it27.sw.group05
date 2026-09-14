using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.DeleteOrder
{
    public interface IDeleteOrderUseCase
    {
        Task<Result> ExecuteAsync(
            DeleteOrderCommand command,
            CancellationToken cancellationToken = default);
    }
}
