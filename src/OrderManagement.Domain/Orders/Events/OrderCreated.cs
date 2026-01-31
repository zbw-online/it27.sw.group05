using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.SeedWork; // Wichtig: Hier liegt DomainEvent laut deiner Info

namespace OrderManagement.Domain.Orders.Events;

// Wir erben vom abstrakten record und reichen das Datum an die Basisklasse weiter
public record OrderCreated(OrderId OrderId, DateTime OccurredOnUtc)
    : DomainEvent(OccurredOnUtc);
