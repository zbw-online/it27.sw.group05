using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Domain.Catalog.ValueObjects;
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
                .Include(o => o.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id, ct);

        public async Task<IReadOnlyList<Order>> GetListAsync(CancellationToken ct = default)
            => await _context.Set<Order>()
                .Include(o => o.Lines)
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<Order?> GetByOrderNumberAsync(OrderNumber orderNumber, CancellationToken cancellationToken = default)
            => await _context.Set<Order>()
                .Include(o => o.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default)
            => await _context.Set<Order>()
                .Include(o => o.Lines)
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken = default)
            => await GetListAsync(cancellationToken);

        public async Task<IReadOnlyList<Order>> GetUnreconciledOrdersAsync(CancellationToken cancellationToken = default)
            => await _context.Set<Order>()
                .Include(o => o.Lines)
                .AsNoTracking()
                .Where(o => !o.IsInventoryApplied)
                .ToListAsync(cancellationToken);

        public async Task<bool> ExistsOrderLineForArticleAsync(ArticleId articleId, CancellationToken cancellationToken = default)
            => await _context.Set<OrderLine>()
                .AsNoTracking()
                .AnyAsync(l => l.ArticleId == articleId, cancellationToken);
    }
}
