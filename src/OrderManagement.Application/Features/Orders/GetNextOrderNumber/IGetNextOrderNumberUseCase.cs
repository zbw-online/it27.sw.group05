using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetNextOrderNumber
{
    public interface IGetNextOrderNumberUseCase
    {
        Task<Result<string>> ExecuteAsync(
            GetNextOrderNumberQuery query,
            CancellationToken cancellationToken = default);
    }
}
