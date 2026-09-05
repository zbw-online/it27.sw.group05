using OrderManagement.Application.Features.Customers.DataExchange.Shared;

namespace OrderManagement.Application.Abstractions.Interfaces.Customers.Query
{
    public interface ICustomerTemporalQueryRepository
    {
        Task<IReadOnlyList<CustomerDataDto>> GetCustomersAsOfAsync(
            DateTime asOfUtc,
            DateOnly asOfBusinessDate,
            CancellationToken cancellationToken = default);
    }
}
