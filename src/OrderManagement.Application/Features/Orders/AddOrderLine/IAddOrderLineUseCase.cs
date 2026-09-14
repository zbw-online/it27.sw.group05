using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.AddOrderLine
{
    public interface IAddOrderLineUseCase
    {
        Task<Result> ExecuteAsync(
            AddOrderLineCommand command,
            CancellationToken cancellationToken = default);
    }
}
