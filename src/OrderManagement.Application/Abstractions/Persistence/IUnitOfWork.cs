using SharedKernel.Primitives;

namespace OrderManagement.Application.Abstractions.Persistence
{
    public interface IUnitOfWork
    {
        Task<Result> CommitAsync(CancellationToken cancellationToken = default);
    }
}
