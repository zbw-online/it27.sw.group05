using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Application.Tests.Fakes.Catalog
{
    public sealed class FakeArticleCommandRepository : IArticleCommandRepository
    {
        private readonly Dictionary<ArticleId, Article> _articles = [];
        private int _nextId = 1;

        public List<Article> Added { get; } = [];
        public List<Article> Updated { get; } = [];
        public List<Article> Removed { get; } = [];

        public Article Seed(Article article)
        {
            if (!article.Id.IsAssigned)
            {
                TestIdAssigner.Assign(article, new ArticleId(_nextId));
            }

            _nextId = Math.Max(_nextId, article.Id.Value + 1);
            _articles[article.Id] = article;
            return article;
        }

        public void Add(Article article)
        {
            var id = new ArticleId(_nextId++);
            TestIdAssigner.Assign(article, id);
            _articles[id] = article;
            Added.Add(article);
        }

        public void Update(Article article)
        {
            _articles[article.Id] = article;
            Updated.Add(article);
        }

        public void Remove(Article article)
        {
            _ = _articles.Remove(article.Id);
            Removed.Add(article);
        }

        public Task<Article?> GetByIdAsync(ArticleId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_articles.GetValueOrDefault(id));
    }
}
