using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Orders.Events
{
    public record OrderStartedProcessing(
        OrderNumber OrderNumber,
        DateTime OccurredOnUtc
        )
        : DomainEvent(OccurredOnUtc);
}
