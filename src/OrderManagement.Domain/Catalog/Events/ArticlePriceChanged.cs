using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticlePriceChanged(
        ArticleId ArticleId,
        Money OldPrice,
        Money NewPrice,
        DateTime OccuredOnUtc
        ) : DomainEvent(OccuredOnUtc);
}
