using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.PreviewAddressForDate
{
    public interface IPreviewAddressForDateUseCase
    {
        Task<Result<PreviewAddressForDateResponse>> ExecuteAsync(
            PreviewAddressForDateQuery query,
            CancellationToken cancellationToken = default);
    }
}
