using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog
{
    public class ArticleRepository(OrderManagementDbContext context) : IArticleRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        // Inherited from IRepository<Article, ArticleId>
        public async Task<Article?> GetByIdAsync(
            ArticleId id,
            CancellationToken ct = default) => await _context.Set<Article>()
                .FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task<IReadOnlyList<Article>> GetListAsync(
            CancellationToken ct = default) => await _context.Set<Article>()
                .ToListAsync(ct);

        public void Add(Article article) => _context.Set<Article>().Add(article);

        public void Update(Article article) => _context.Set<Article>().Update(article);

        public void Remove(Article article) => _context.Set<Article>().Remove(article);

        // Article-specific methods (from IArticleRepository)
        public async Task<Article?> GetByNumberAsync(
            ArticleNumber number,
            CancellationToken cancellationToken = default) => await _context.Set<Article>()
                .FirstOrDefaultAsync(
                    a => a.ArticleNumber.Equals(number),
                    cancellationToken);

        public async Task<IReadOnlyList<Article>> GetByGroupAsync(
            ArticleGroupId groupId,
            CancellationToken cancellationToken = default) => await _context.Set<Article>()
                .Where(a => a.ArticleGroupId == groupId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<Article>> GetLowStockAsync(
            int threshold,
            CancellationToken cancellationToken = default) => await _context.Set<Article>()
                .Where(a => a.Stock < threshold && a.Status == 1) // Active only
                .ToListAsync(cancellationToken);
    }
}
