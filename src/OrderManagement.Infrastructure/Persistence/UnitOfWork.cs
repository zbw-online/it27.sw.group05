using OrderManagement.Application.Abstractions;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Persistence
{
    //public class UnitOfWork(OrderManagementDbContext context) : IUnitOfWork
    //{
    //    private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    //    public async Task<Result> CommitAsync(CancellationToken cancellationToken = default)
    //    {
    //        try
    //        {
    //            _ = await _context.SaveChangesAsync(cancellationToken);
    //            return Result.Success();
    //        }
    //        catch (Exception ex)
    //        {
    //            return Result.Fail(ex.Message);
    //        }
    //    }
    //}
}
