using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Catalog;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog
{
    public class ArticleGroupRepository(OrderManagementDbContext context) : IArticleGroupRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        // Inherited from IRepository<ArticleGroup, ArticleGroupId>
        public async Task<ArticleGroup?> GetByIdAsync(
            ArticleGroupId id,
            CancellationToken ct = default) => await _context.Set<ArticleGroup>()
                .FirstOrDefaultAsync(g => g.Id == id, ct);

        public async Task<IReadOnlyList<ArticleGroup>> GetListAsync(
            CancellationToken ct = default) => await _context.Set<ArticleGroup>()
                .ToListAsync(ct);

        public void Add(ArticleGroup group) => _context.Set<ArticleGroup>().Add(group);

        public void Update(ArticleGroup group) => _context.Set<ArticleGroup>().Update(group);

        public void Remove(ArticleGroup group) => _context.Set<ArticleGroup>().Remove(group);

        // ArticleGroup-specific methods (from IArticleGroupRepository)
        public async Task<ArticleGroup?> GetByIdWithChildrenAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default) => await _context.Set<ArticleGroup>()
                .Include(g => g.Children)  // Navigation property to child groups
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        public async Task<IReadOnlyList<ArticleGroup>> GetByParentAsync(
            ArticleGroupId? parentId,
            CancellationToken cancellationToken = default) => parentId.HasValue
                ? await _context.Set<ArticleGroup>()
                    .Where(g => g.ParentGroupId == parentId)
                    .ToListAsync(cancellationToken)
                : await _context.Set<ArticleGroup>()
                    .Where(g => g.ParentGroupId == null)
                    .ToListAsync(cancellationToken);
    }
}
