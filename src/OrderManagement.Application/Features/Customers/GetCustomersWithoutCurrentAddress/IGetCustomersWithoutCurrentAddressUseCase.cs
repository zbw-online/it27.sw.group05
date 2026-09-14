using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.GetCustomersWithoutCurrentAddress
{
    public interface IGetCustomersWithoutCurrentAddressUseCase
    {
        Task<Result<IReadOnlyList<CustomerWithoutAddressDto>>> ExecuteAsync(
            GetCustomersWithoutCurrentAddressQuery query,
            CancellationToken cancellationToken = default);
    }
}
