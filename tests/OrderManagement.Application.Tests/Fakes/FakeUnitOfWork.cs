using OrderManagement.Application.Abstractions;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Fakes
{
    public sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCount { get; private set; }
        public string? FailureMessage { get; set; }

        public Task<Result> CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCount++;

            return Task.FromResult(FailureMessage is null
                ? Result.Success()
                : Result.Fail(FailureMessage));
        }
    }
}
