using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticleGroupRenamed(
        string OldName,
        string Name,
        DateTime OccuredOnUtc
        ) : DomainEvent(OccuredOnUtc);
}
