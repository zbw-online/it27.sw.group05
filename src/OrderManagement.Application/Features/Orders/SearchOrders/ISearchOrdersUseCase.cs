using OrderManagement.Application.Features.Orders.Shared;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.SearchOrders
{
    public interface ISearchOrdersUseCase
    {
        Task<Result<IReadOnlyList<OrderListItemDto>>> ExecuteAsync(
            SearchOrdersQuery query,
            CancellationToken cancellationToken = default);
    }
}
