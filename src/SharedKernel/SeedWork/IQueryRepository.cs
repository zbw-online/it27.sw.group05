namespace SharedKernel.SeedWork
{
    public interface IQueryRepository<T, TId>
        where T : AggregateRoot<TId>
        where TId : notnull
    {
        Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
        Task<IReadOnlyList<T>> GetListAsync(CancellationToken ct = default);
    }
}
