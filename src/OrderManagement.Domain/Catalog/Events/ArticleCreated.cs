using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticleCreated(ArticleId ArticleId, DateTime OccuredOnUtc)
    : DomainEvent(OccuredOnUtc);
}
