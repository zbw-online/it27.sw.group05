using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticleGroupCreated(
        string Name,
        DateTime OccuredOnUtc
        ) : DomainEvent(OccuredOnUtc);
}
