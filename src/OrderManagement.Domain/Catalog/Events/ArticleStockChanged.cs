using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog.Events
{
    public sealed record ArticleStockChanged(
        ArticleNumber ArticleNumber,
        int OldStock,
        int NewStock,
        DateTime OccuredOnUtc
        ) : DomainEvent(OccuredOnUtc);
}
