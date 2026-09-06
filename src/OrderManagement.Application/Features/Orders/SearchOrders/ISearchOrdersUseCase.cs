using OrderManagement.Application.Features.Orders.Contracts;

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
