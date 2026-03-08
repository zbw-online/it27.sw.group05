using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Interfaces.Orders.Query
{
    public interface IOrderQueryRepository : IQueryRepository<Order, OrderId>
    {
        Task<Order?> GetByOrderNumberAsync(
            OrderNumber orderNumber,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Order>> GetByCustomerIdAsync(
            CustomerId customerId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Order>> GetPendingOrdersAsync(
            CancellationToken cancellationToken = default);
    }
}
