using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

namespace OrderManagement.Application.Abstractions.Persistence.Customers.Query
{
    public interface ICustomerTemporalQueryRepository
    {
        Task<IReadOnlyList<CustomerDataDto>> GetCustomersAsOfAsync(
            DateTime asOfUtc,
            DateOnly asOfBusinessDate,
            CancellationToken cancellationToken = default);
    }
}
