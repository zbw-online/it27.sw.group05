using OrderManagement.Application.Features.Orders.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.SearchArchivedOrders
{
    public interface ISearchArchivedOrdersUseCase
    {
        Task<Result<OrderSearchResultDto>> ExecuteAsync(
            SearchArchivedOrdersQuery query,
            CancellationToken cancellationToken = default);
    }
}
