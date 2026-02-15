using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Domain.Catalog;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command
{
    public class ArticleCommandRepository(OrderManagementDbContext context) : IArticleCommandRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public void Add(Article article)
            => _context.Set<Article>().Add(article);

        public void Update(Article article)
            => _context.Set<Article>().Update(article);

        public void Remove(Article article)
            => _context.Set<Article>().Remove(article);
    }
}
