using OrderManagement.Application.Features.Customers.Shared;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.SearchCustomers
{
    public interface ISearchCustomersUseCase
    {
        Task<Result<IReadOnlyList<CustomerListItemDto>>> ExecuteAsync(
            SearchCustomersQuery query,
            CancellationToken cancellationToken = default);
    }
}
