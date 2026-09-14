using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Persistence;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Persistence
{
    public class UnitOfWork(OrderManagementDbContext context) : IUnitOfWork
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<Result> CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _ = await _context.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail("Der Lagerbestand wurde zwischenzeitlich geändert. Bitte laden Sie die Artikel erneut und prüfen Sie die Mengen.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return Result.Fail("Die Änderung konnte nicht gespeichert werden. Bitte versuchen Sie es erneut.");
            }
        }
    }
}
