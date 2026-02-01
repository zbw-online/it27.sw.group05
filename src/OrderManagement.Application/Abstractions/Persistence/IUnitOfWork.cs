

namespace OrderManagement.Application.Abstractions.Persistence
{
    public interface IUnitofWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
