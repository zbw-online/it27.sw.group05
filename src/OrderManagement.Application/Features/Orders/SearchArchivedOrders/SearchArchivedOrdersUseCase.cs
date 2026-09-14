using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Application.Features.Orders.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.SearchArchivedOrders
{
    public sealed class SearchArchivedOrdersUseCase(
        IOrderSearchQueryRepository orderSearchQueryRepository,
        TimeProvider timeProvider) : ISearchArchivedOrdersUseCase
    {
        private const int MaxPageSize = 100;

        private readonly IOrderSearchQueryRepository _orderSearchQueryRepository = orderSearchQueryRepository;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<Result<OrderSearchResultDto>> ExecuteAsync(
            SearchArchivedOrdersQuery query,
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

            var criteria = new OrderSearchCriteria(
                OrderSearchScope.Archived,
                query.SearchTerm,
                Math.Max(1, query.Page),
                Math.Clamp(query.PageSize, 1, MaxPageSize),
                today);

            OrderSearchResultDto result = await _orderSearchQueryRepository.SearchAsync(criteria, cancellationToken);
            return Results.Success(result);
        }
    }
}
