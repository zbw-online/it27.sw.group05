using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Application.Features.Orders.Contracts;

namespace OrderManagement.Application.Tests.Fakes.Orders
{
    public sealed class FakeOrderSearchQueryRepository : IOrderSearchQueryRepository
    {
        public OrderSearchResultDto ResultToReturn { get; set; } = new([], 0, 1, 15, 0);
        public OrderSearchCriteria? CapturedCriteria { get; private set; }
        public int CallCount { get; private set; }

        public Task<OrderSearchResultDto> SearchAsync(
            OrderSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CapturedCriteria = criteria;
            CallCount++;
            return Task.FromResult(ResultToReturn);
        }
    }
}
