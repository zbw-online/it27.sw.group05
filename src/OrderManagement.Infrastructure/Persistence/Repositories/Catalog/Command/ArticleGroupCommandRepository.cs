using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Domain.Catalog;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command
{
    public class ArticleGroupCommandRepository(OrderManagementDbContext context) : IArticleGroupCommandRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public void Add(ArticleGroup articleGroup)
            => _context.Set<ArticleGroup>().Add(articleGroup);

        public void Update(ArticleGroup articleGroup)
            => _context.Set<ArticleGroup>().Update(articleGroup);

        public void Remove(ArticleGroup articleGroup)
            => _context.Set<ArticleGroup>().Remove(articleGroup);
    }
}
