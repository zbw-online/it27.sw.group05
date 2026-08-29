using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Application.Tests.Fakes.Catalog
{
    public sealed class FakeArticleGroupCommandRepository : IArticleGroupCommandRepository
    {
        private readonly Dictionary<ArticleGroupId, ArticleGroup> _groups = [];
        private int _nextId = 1;

        public List<ArticleGroup> Added { get; } = [];
        public List<ArticleGroup> Updated { get; } = [];
        public List<ArticleGroup> Removed { get; } = [];

        public ArticleGroup Seed(ArticleGroup group)
        {
            if (!group.Id.IsAssigned)
            {
                TestIdAssigner.Assign(group, new ArticleGroupId(_nextId));
            }

            _nextId = Math.Max(_nextId, group.Id.Value + 1);
            _groups[group.Id] = group;
            return group;
        }

        public void Add(ArticleGroup group)
        {
            var id = new ArticleGroupId(_nextId++);
            TestIdAssigner.Assign(group, id);
            _groups[id] = group;
            Added.Add(group);
        }

        public void Update(ArticleGroup group)
        {
            _groups[group.Id] = group;
            Updated.Add(group);
        }

        public void Remove(ArticleGroup group)
        {
            _ = _groups.Remove(group.Id);
            Removed.Add(group);
        }

        public Task<ArticleGroup?> GetByIdAsync(ArticleGroupId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_groups.GetValueOrDefault(id));

        public Task<ArticleGroup?> GetByIdWithChildrenAsync(ArticleGroupId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_groups.GetValueOrDefault(id));
    }
}
