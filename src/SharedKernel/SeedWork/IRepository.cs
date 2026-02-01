namespace SharedKernel.SeedWork
{
    public interface IRepository<T, TId> where T : AggregateRoot<TId> where TId : notnull
    {
        Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
        Task<IReadOnlyList<T>> GetListAsync(CancellationToken ct = default);
        void Add(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
