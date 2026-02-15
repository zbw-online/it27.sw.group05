using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query
{
    public class ArticleGroupQueryRepository(OrderManagementDbContext context) : IArticleGroupQueryRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<ArticleGroup?> GetByIdAsync(
            ArticleGroupId id,
            CancellationToken ct = default)
            => await _context.Set<ArticleGroup>()
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id, ct);

        public async Task<IReadOnlyList<ArticleGroup>> GetListAsync(
            CancellationToken ct = default)
            => await _context.Set<ArticleGroup>()
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<ArticleGroup?> GetByIdWithChildrenAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default)
            => await _context.Set<ArticleGroup>()
                .AsNoTracking()
                .Include(g => g.Children)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        public async Task<IReadOnlyList<ArticleGroup>> GetByParentAsync(
            ArticleGroupId? parentId,
            CancellationToken cancellationToken = default)
            => await _context.Set<ArticleGroup>()
                .AsNoTracking()
                .Where(g => g.ParentGroupId == parentId)
                .ToListAsync(cancellationToken);
    }
}
