using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

namespace OrderManagement.Application.Abstractions.Persistence.Customers.Query
{
    public interface ICustomerCurrentQueryRepository
    {
        Task<IReadOnlyList<CustomerDataDto>> GetAllCurrentAsync(
            DateOnly asOfBusinessDate,
            CancellationToken cancellationToken = default);
    }
}
