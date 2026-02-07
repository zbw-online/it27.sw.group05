using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticleStockChanged(
        ArticleId ArticleId,
        int OldStock,
        int NewStock,
        DateTime OccuredOnUtc
        ) : DomainEvent(OccuredOnUtc);
}
