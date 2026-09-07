using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Orders.Events
{
    public record OrderCancelled(
        OrderNumber OrderNumber,
        DateTime OccurredOnUtc,
        bool InventoryReversalRequired
        )
        : DomainEvent(OccurredOnUtc);
}
