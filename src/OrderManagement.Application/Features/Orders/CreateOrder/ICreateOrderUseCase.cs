using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.CreateOrder
{
    public interface ICreateOrderUseCase
    {
        Task<Result<CreateOrderResponse>> ExecuteAsync(
            CreateOrderCommand command,
            CancellationToken cancellationToken = default);
    }
}
