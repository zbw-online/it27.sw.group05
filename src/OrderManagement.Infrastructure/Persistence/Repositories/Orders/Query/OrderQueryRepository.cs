using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query
{
    public sealed class OrderQueryRepository(OrderManagementDbContext context) : IOrderQueryRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
            => await _context.Set<Order>()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id, ct);

        public async Task<IReadOnlyList<Order>> GetListAsync(CancellationToken ct = default)
            => await _context.Set<Order>()
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<Order?> GetByOrderNumberAsync(OrderNumber orderNumber, CancellationToken cancellationToken = default)
            => await _context.Set<Order>()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default)
            => await _context.Set<Order>()
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken ct = default)
            => await GetListAsync(ct);
    }
}
