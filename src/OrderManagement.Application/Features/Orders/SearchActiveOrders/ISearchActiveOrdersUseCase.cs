using OrderManagement.Application.Features.Orders.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.SearchActiveOrders
{
    public interface ISearchActiveOrdersUseCase
    {
        Task<Result<OrderSearchResultDto>> ExecuteAsync(
            SearchActiveOrdersQuery query,
            CancellationToken cancellationToken = default);
    }
}
