namespace SharedKernel.SeedWork
{
    public abstract class AggregateRoot<TId> : Entity<TId>
        where TId : notnull
    {

        private readonly List<DomainEvent> _domainEvents = [];

        protected AggregateRoot(TId id) : base(id) { }
        protected AggregateRoot() { }

        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
