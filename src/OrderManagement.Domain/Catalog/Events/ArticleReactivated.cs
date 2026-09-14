using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticleReactivated(
        ArticleNumber ArticleNumber,
        DateTime OccuredOnUtc
        ) : DomainEvent(OccuredOnUtc);
}
