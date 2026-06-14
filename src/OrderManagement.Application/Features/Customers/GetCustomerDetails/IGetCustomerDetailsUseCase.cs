using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.GetCustomerDetails
{
    public interface IGetCustomerDetailsUseCase
    {
        Task<Result<GetCustomerDetailsResponse>> ExecuteAsync(
            GetCustomerDetailsQuery query,
            CancellationToken cancellationToken = default);
    }
}
