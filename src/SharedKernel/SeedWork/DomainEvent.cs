namespace SharedKernel.SeedWork
{
    public abstract record DomainEvent(DateTime OccurredOnUtc)
    {
        public DateTime OccuredOnUtc { get; init; } = OccurredOnUtc;
    }
}
