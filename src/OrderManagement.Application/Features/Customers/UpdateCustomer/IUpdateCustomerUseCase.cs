using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.UpdateCustomer
{
    public interface IUpdateCustomerUseCase
    {
        Task<Result> ExecuteAsync(
            UpdateCustomerCommand command,
            CancellationToken cancellationToken = default);
    }
}
