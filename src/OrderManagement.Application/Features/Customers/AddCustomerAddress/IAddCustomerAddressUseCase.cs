using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.AddCustomerAddress
{
    public interface IAddCustomerAddressUseCase
    {
        Task<Result> ExecuteAsync(
            AddCustomerAddressCommand command,
            CancellationToken cancellationToken = default);
    }
}
