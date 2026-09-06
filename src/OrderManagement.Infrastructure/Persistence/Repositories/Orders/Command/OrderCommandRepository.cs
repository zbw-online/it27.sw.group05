using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Persistence.Orders.Command;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

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

        public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
            => await _context.Set<Order>()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
}
