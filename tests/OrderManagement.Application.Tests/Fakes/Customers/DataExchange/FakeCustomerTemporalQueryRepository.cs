using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Application.Features.Customers.DataExchange.Shared;

namespace OrderManagement.Application.Tests.Fakes.Customers.DataExchange
{
    public sealed class FakeCustomerTemporalQueryRepository : ICustomerTemporalQueryRepository
    {
        public IReadOnlyList<CustomerDataDto> CustomersToReturn { get; set; } = [];
        public DateTime? CapturedAsOfUtc { get; private set; }
        public DateOnly? CapturedAsOfBusinessDate { get; private set; }

        public Task<IReadOnlyList<CustomerDataDto>> GetCustomersAsOfAsync(
            DateTime asOfUtc,
            DateOnly asOfBusinessDate,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CapturedAsOfUtc = asOfUtc;
            CapturedAsOfBusinessDate = asOfBusinessDate;
            return Task.FromResult(CustomersToReturn);
        }
    }
}
