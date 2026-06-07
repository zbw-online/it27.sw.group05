using OrderManagement.Domain.Catalog.Events;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog
{
    public sealed class ArticleGroup : AggregateRoot<ArticleGroupId>
    {
        private readonly List<ArticleGroup> _children = [];
        private ArticleGroup() : base(ArticleGroupId.Empty)
        {
            // EF Core
        }

        private ArticleGroup(string name) : base(ArticleGroupId.Empty)
        {
            Name = name;
            AddDomainEvent(new ArticleGroupCreated(name, DateTime.UtcNow));
        }

        public string Name { get; private set; } = default!;
        public ArticleGroupId? ParentGroupId { get; private set; }
        public IReadOnlyCollection<ArticleGroup> Children => _children.AsReadOnly();
        public string? Description { get; private set; }
        public int Status { get; private set; } = 1;

        public static Result<ArticleGroup> Create(
            string? name,
            ArticleGroupId? parentGroupId = null)
        {
            string trimmedName = (name ?? string.Empty).Trim();
            if (trimmedName.Length == 0)
                return Results.Fail<ArticleGroup>("Name is required.");
            if (trimmedName.Length > 150)
                return Results.Fail<ArticleGroup>("Name must not exceed 150 characters.");


            if (parentGroupId.HasValue && !parentGroupId.Value.IsAssigned)
                return Results.Fail<ArticleGroup>("ParentGroupId must be assigned.");


            var group = new ArticleGroup(trimmedName)
            {
                ParentGroupId = parentGroupId
            };

            return Results.Success(group);
        }

        public Result AddChild(ArticleGroup child)
        {

            if (!Id.IsAssigned)
                return Result.Fail("Parent group must be persisted before children can be attached.");

            if (!child.ParentGroupId.HasValue || child.ParentGroupId.Value != Id)
                return Result.Fail("Child group does not reference this group as parent.");

            if (_children.Contains(child))
                return Result.Fail("Child already exists.");


            _children.Add(child);
            return Result.Success();
        }

        public Result Rename(string newName)
        {
            string trimmedName = (newName ?? string.Empty).Trim();

            if (trimmedName.Length == 0)
                return Result.Fail("Name is required.");
            if (trimmedName.Length > 150)
                return Result.Fail("Name must not exced 150 characters.");

            string oldName = Name;
            Name = trimmedName;
            AddDomainEvent(new ArticleGroupRenamed(oldName, trimmedName, DateTime.UtcNow));

            return Result.Success();
        }
    }
}
