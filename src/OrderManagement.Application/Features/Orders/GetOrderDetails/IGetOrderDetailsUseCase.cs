using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetOrderDetails
{
    public interface IGetOrderDetailsUseCase
    {
        Task<Result<GetOrderDetailsResponse>> ExecuteAsync(
            GetOrderDetailsQuery query,
            CancellationToken cancellationToken = default);
    }
}
