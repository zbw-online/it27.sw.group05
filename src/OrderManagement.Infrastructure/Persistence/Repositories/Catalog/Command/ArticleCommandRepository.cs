using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command
{
    public class ArticleCommandRepository(OrderManagementDbContext context) : IArticleCommandRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<Article?> GetByIdAsync(
            ArticleId id,
            CancellationToken cancellationToken = default)
            => await _context.Articles
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        public async Task<IReadOnlyList<Article>> GetByIdsAsync(
            IReadOnlyCollection<ArticleId> ids,
            CancellationToken cancellationToken = default)
            => await _context.Articles
                .Where(a => ids.Contains(a.Id))
                .ToListAsync(cancellationToken);

        public void Add(Article article)
            => _context.Set<Article>().Add(article);

        public void Update(Article article)
            => _context.Set<Article>().Update(article);

        public void Remove(Article article)
            => _context.Set<Article>().Remove(article);
    }
}
