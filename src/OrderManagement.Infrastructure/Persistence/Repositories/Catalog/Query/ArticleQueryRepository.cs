using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query
{
    public class ArticleQueryRepository(OrderManagementDbContext context) : IArticleQueryRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<Article?> GetByIdAsync(
            ArticleId id,
            CancellationToken ct = default)
            => await _context.Set<Article>()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task<IReadOnlyList<Article>> GetListAsync(
            CancellationToken ct = default)
            => await _context.Set<Article>()
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<Article?> GetByNumberAsync(
            ArticleNumber number,
            CancellationToken cancellationToken = default)
            => await _context.Set<Article>()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.ArticleNumber.Value == number.Value,
                    cancellationToken);

        public async Task<IReadOnlyList<Article>> GetByGroupAsync(
            ArticleGroupId groupId,
            CancellationToken cancellationToken = default)
            => await _context.Set<Article>()
                .AsNoTracking()
                .Where(a => a.ArticleGroupId == groupId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Article>> GetLowStockAsync(
            int threshold,
            CancellationToken cancellationToken = default)
            => await _context.Set<Article>()
                .AsNoTracking()
                .Where(a => a.Stock < threshold && a.Status == 1)
                .ToListAsync(cancellationToken);
    }
}
