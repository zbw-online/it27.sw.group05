using OrderManagement.Domain.Catalog.Events;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog
{
    public sealed class ArticleGroup : AggregateRoot<ArticleGroupId>
    {
        private readonly List<ArticleGroup> _children = [];
        private ArticleGroup() : base(new ArticleGroupId(0)) { }

        private ArticleGroup(
            ArticleGroupId id,
            string name
            ) : base(id)
        {
            Name = name;
            AddDomainEvent(new ArticleGroupCreated(id, DateTime.UtcNow));
        }

        public string Name { get; private set; } = default!;
        public ArticleGroupId? ParentGroupId { get; private set; }
        public IReadOnlyCollection<ArticleGroup> Children => _children.AsReadOnly();
        public string? Description { get; private set; }
        public int Status { get; private set; } = 1;

        public static Result<ArticleGroup> Create(
            int id,
            string? name,
            int? parentGroupId = null
            )
        {
            if (id <= 0) return Results.Fail<ArticleGroup>("ArticleGroup id must be positive.");

            string trimmedName = (name ?? string.Empty).Trim();
            if (trimmedName.Length == 0)
                return Results.Fail<ArticleGroup>("Name is required.");

            if (trimmedName.Length > 150)
                return Results.Fail<ArticleGroup>("Name must not exceed 150 characters.");

            if (parentGroupId.HasValue && parentGroupId <= 0)
                return Results.Fail<ArticleGroup>("ParentGroupId must be positive.");

            var group = new ArticleGroup(new ArticleGroupId(id), trimmedName);

            if (parentGroupId.HasValue)
                group.ParentGroupId = new ArticleGroupId(parentGroupId.Value);

            return Results.Success(group);
        }

        public Result AddChild(ArticleGroup child)
        {
            if (_children.Contains(child))
                return Result.Fail("Child already exists.");

            if (HasCircularReference(child))
                return Result.Fail("Cannot create circular group hierarchy.");

            _children.Add(child);
            return Result.Success();
        }

        private bool HasCircularReference(ArticleGroup potentialChild)
        {
            var visited = new HashSet<int> { Id.Value };
            ArticleGroup? current = this;

            while (current?.ParentGroupId.HasValue == true)
            {
                int parentId = current.ParentGroupId.Value.Value;

                if (parentId == potentialChild.Id.Value)
                    return true; // potentialChild is ancestor

                if (visited.Contains(parentId))
                    return true; // cycle detected

                _ = visited.Add(parentId);
                current = null!; // In real app, load parent from repo
            }

            return false;
        }


        public Result Rename(string newName)
        {
            string trimmedName = (newName ?? string.Empty).Trim();
            if (trimmedName.Length == 0)
                return Result.Fail("Name is required.");

            Name = trimmedName;
            AddDomainEvent(new ArticleGroupRenamed(Id, DateTime.UtcNow));
            return Result.Success();
        }
    }
}
