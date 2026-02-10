using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Orders.Events
{

    public record OrderCreated(OrderId OrderId, DateTime OccurredOnUtc)
        : DomainEvent(OccurredOnUtc);
}
