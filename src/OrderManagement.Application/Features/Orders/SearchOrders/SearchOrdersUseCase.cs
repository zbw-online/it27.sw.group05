using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.SearchOrders
{
    public sealed class SearchOrdersUseCase(
        IOrderQueryRepository orderQueryRepository,
        ICustomerQueryRepository customerQueryRepository) : ISearchOrdersUseCase
    {
        private readonly IOrderQueryRepository _orderQueryRepository = orderQueryRepository;
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;

        public async Task<Result<IReadOnlyList<OrderListItemDto>>> ExecuteAsync(
            SearchOrdersQuery query,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Order> orders = await _orderQueryRepository.GetListAsync(cancellationToken);
            IReadOnlyList<Customer> customers = await _customerQueryRepository.GetListAsync(cancellationToken);

            var customerNumberById = customers.ToDictionary(c => c.Id.Value, c => c.CustomerNumber.Value);

            string term = (query.SearchTerm ?? string.Empty).Trim();

            if (term.Length > 0)
            {
                orders = [.. orders.Where(o =>
                    o.OrderNumber.Value.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (customerNumberById.TryGetValue(o.CustomerId.Value, out string? number) &&
                        number.Contains(term, StringComparison.OrdinalIgnoreCase)))];
            }

            IReadOnlyList<OrderListItemDto> result = [.. orders
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderListItemDto(
                    o.Id.Value,
                    o.OrderNumber.Value,
                    o.OrderDate,
                    o.CustomerId.Value,
                    customerNumberById.TryGetValue(o.CustomerId.Value, out string? cn) ? cn : string.Empty,
                    o.Lines.Count,
                    o.Total.Amount,
                    o.Total.Currency))];

            return Results.Success(result);
        }
    }
}
