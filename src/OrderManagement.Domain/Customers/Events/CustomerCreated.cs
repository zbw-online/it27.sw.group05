using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Customers.Events
{
    public sealed record CustomerCreated(CustomerNumber CustomerNumber, DateTime OccuredOnUtc) : DomainEvent(OccuredOnUtc);
}
