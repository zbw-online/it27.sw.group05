using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Application.Tests.Fakes.Catalog
{
    public sealed class FakeArticleQueryRepository : IArticleQueryRepository
    {
        private readonly List<Article> _articles = [];
        private int _nextId = 1;

        public Article Seed(Article article)
        {
            if (!article.Id.IsAssigned)
            {
                TestIdAssigner.Assign(article, new ArticleId(_nextId));
            }

            _nextId = Math.Max(_nextId, article.Id.Value + 1);
            _articles.Add(article);
            return article;
        }

        public Task<Article?> GetByIdAsync(ArticleId id, CancellationToken ct = default)
            => Task.FromResult(_articles.FirstOrDefault(a => a.Id == id));

        public Task<IReadOnlyList<Article>> GetListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Article>>([.. _articles]);

        public Task<Article?> GetByNumberAsync(ArticleNumber number, CancellationToken cancellationToken = default)
            => Task.FromResult(_articles.FirstOrDefault(a => a.ArticleNumber == number));

        public Task<IReadOnlyList<Article>> GetByGroupAsync(ArticleGroupId groupId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Article>>([.. _articles.Where(a => a.ArticleGroupId == groupId)]);

        public Task<IReadOnlyList<Article>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Article>>([.. _articles.Where(a => a.Stock <= threshold)]);
    }
}
