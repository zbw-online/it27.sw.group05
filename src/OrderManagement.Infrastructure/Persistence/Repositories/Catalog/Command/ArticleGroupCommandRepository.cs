using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command
{
    public class ArticleGroupCommandRepository(OrderManagementDbContext context) : IArticleGroupCommandRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<ArticleGroup?> GetByIdAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default)
            => await _context.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        public async Task<ArticleGroup?> GetByIdWithChildrenAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default)
            => await _context.ArticleGroups
                .Include(g => g.Children)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        public void Add(ArticleGroup articleGroup)
            => _context.Set<ArticleGroup>().Add(articleGroup);

        public void Update(ArticleGroup articleGroup)
            => _context.Set<ArticleGroup>().Update(articleGroup);

        public void Remove(ArticleGroup articleGroup)
            => _context.Set<ArticleGroup>().Remove(articleGroup);
    }
}
