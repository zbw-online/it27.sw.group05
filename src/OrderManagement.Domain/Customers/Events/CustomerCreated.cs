using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Customers.Events
{
    public sealed record CustomerCreated(CustomerId CustomerId, DateTime OccuredOnUtc) : DomainEvent(OccuredOnUtc);
}
