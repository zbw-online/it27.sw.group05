using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.DeleteCustomer
{
    public interface IDeleteCustomerUseCase
    {
        Task<Result> ExecuteAsync(
            DeleteCustomerCommand command,
            CancellationToken cancellationToken = default);
    }
}
