using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Orders.Command
{
    public sealed class OrderCommandRepository(OrderManagementDbContext context) : IOrderCommandRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public void Add(Order order)
            => _context.Set<Order>().Add(order);

        public void Update(Order order)
            => _context.Set<Order>().Update(order);

        public void Remove(Order order)
            => _context.Set<Order>().Remove(order);
    }
}
