using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Persistence.Initialization
{
    public interface IDatabaseInitializer
    {
        Task<Result> InitializeAsync(CancellationToken cancellationToken = default);
    }
}
