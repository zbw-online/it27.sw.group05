using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Persistence.Orders.Command
{
    public interface IOrderCommandRepository : ICommandRepository<Order, OrderId>
    {
        Task<Order?> GetByIdAsync(
            OrderId id,
            CancellationToken cancellationToken = default);
    }
}
