using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticleReorderPointChanged(
        ArticleNumber ArticleNumber,
        int OldReorderPoint,
        int NewReorderPoint,
        DateTime OccuredOnUtc
        ) : DomainEvent(OccuredOnUtc);
}
