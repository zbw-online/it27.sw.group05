using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.StartOrderProcessing
{
    public interface IStartOrderProcessingUseCase
    {
        Task<Result> ExecuteAsync(
            StartOrderProcessingCommand command,
            CancellationToken cancellationToken = default);
    }
}
