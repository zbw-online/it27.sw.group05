using OrderManagement.Application.Abstractions.Persistence.Catalog.Query;
using OrderManagement.Application.Features.Catalog.Contracts;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Application.Tests.Fakes.Catalog
{
    public sealed class FakeArticleGroupQueryRepository : IArticleGroupQueryRepository
    {
        private readonly List<ArticleGroup> _groups = [];
        private int _nextId = 1;

        public ArticleGroup Seed(ArticleGroup group)
        {
            if (!group.Id.IsAssigned)
            {
                TestIdAssigner.Assign(group, new ArticleGroupId(_nextId));
            }

            _nextId = Math.Max(_nextId, group.Id.Value + 1);
            _groups.Add(group);
            return group;
        }

        public Task<ArticleGroup?> GetByIdAsync(ArticleGroupId id, CancellationToken ct = default)
            => Task.FromResult(_groups.FirstOrDefault(g => g.Id == id));

        public Task<IReadOnlyList<ArticleGroup>> GetListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ArticleGroup>>([.. _groups]);

        public Task<ArticleGroup?> GetByIdWithChildrenAsync(ArticleGroupId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_groups.FirstOrDefault(g => g.Id == id));

        public Task<IReadOnlyList<ArticleGroup>> GetByParentAsync(ArticleGroupId? parentId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ArticleGroup>>([.. _groups.Where(g => g.ParentGroupId == parentId)]);

        public IReadOnlyList<ArticleGroupHierarchyDto> HierarchyResult { get; set; } = [];
        public ArticleGroupId? HierarchyFromRootCalledWith { get; private set; }
        public bool FullHierarchyCalled { get; private set; }

        public Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetHierarchyFromRootAsync(ArticleGroupId rootId, CancellationToken cancellationToken = default)
        {
            HierarchyFromRootCalledWith = rootId;
            return Task.FromResult(HierarchyResult);
        }

        public Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetFullHierarchyAsync(CancellationToken cancellationToken = default)
        {
            FullHierarchyCalled = true;
            return Task.FromResult(HierarchyResult);
        }
    }
}
