using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

namespace OrderManagement.Application.Tests.Fakes.Customers.DataExchange
{
    public sealed class FakeCustomerCurrentQueryRepository : ICustomerCurrentQueryRepository
    {
        public IReadOnlyList<CustomerDataDto> CustomersToReturn { get; set; } = [];
        public DateOnly? CapturedAsOfBusinessDate { get; private set; }
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<CustomerDataDto>> GetAllCurrentAsync(
            DateOnly asOfBusinessDate,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CapturedAsOfBusinessDate = asOfBusinessDate;
            CallCount++;
            return Task.FromResult(CustomersToReturn);
        }
    }
}
