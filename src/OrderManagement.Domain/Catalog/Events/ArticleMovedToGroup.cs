using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticleMovedToGroup(
        ArticleNumber ArticleNumber,
        ArticleGroupId OldGroupId,
        ArticleGroupId NewGroupId,
        DateTime OccuredOnUtc
        ) : DomainEvent(OccuredOnUtc);
}
