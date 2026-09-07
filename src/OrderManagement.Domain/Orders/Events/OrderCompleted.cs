using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Orders.Events
{
    public record OrderCompleted(
        OrderNumber OrderNumber,
        DateTime OccurredOnUtc
        )
        : DomainEvent(OccurredOnUtc);
}
