using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.CreateCustomer
{
    public interface ICreateCustomerUseCase
    {
        Task<Result<CreateCustomerResponse>> ExecuteAsync(
            CreateCustomerCommand command,
            CancellationToken cancellationToken = default);
    }
}
