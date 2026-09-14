using OrderManagement.Application.Features.Orders.Contracts;

namespace OrderManagement.Application.Abstractions.Persistence.Orders.Query
{
    public interface IOrderSearchQueryRepository
    {
        Task<OrderSearchResultDto> SearchAsync(
            OrderSearchCriteria criteria,
            CancellationToken cancellationToken = default);
    }
}
