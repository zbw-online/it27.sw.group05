using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticleGroupRenamed(
        ArticleGroupId ArticleGroupId,
        DateTime OccuredOnUtc
        ) : DomainEvent(OccuredOnUtc);
}
