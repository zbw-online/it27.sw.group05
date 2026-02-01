using SharedKernel.Primitives;

namespace OrderManagement.Application.Abstractions
{
    public interface IUnitOfWork
    {
        Task<Result> CommitAsync(CancellationToken cancellationToken = default);
    }
}
