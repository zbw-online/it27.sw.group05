namespace SharedKernel.SeedWork
{
    public interface ICommandRepository<T, TId>
        where T : AggregateRoot<TId>
        where TId : notnull
    {
        void Add(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
