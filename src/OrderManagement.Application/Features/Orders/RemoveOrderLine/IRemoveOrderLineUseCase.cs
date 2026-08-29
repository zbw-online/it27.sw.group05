using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.RemoveOrderLine
{
    public interface IRemoveOrderLineUseCase
    {
        Task<Result> ExecuteAsync(
            RemoveOrderLineCommand command,
            CancellationToken cancellationToken = default);
    }
}
