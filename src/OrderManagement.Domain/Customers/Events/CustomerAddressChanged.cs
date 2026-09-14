using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Customers.Events
{
    public sealed record CustomerAddressChanged(CustomerNumber CustomerNumber, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
}
