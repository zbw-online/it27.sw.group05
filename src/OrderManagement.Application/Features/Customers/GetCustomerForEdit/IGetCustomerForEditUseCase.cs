using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.GetCustomerForEdit
{
    public interface IGetCustomerForEditUseCase
    {
        Task<Result<GetCustomerForEditResponse>> ExecuteAsync(
            GetCustomerForEditQuery query,
            CancellationToken cancellationToken = default);
    }
}
