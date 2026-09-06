using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Persistence.Initialization
{
    public sealed class DatabaseInitializer(
        OrderManagementDbContext dbContext,
        DemoDataSeeder demoDataSeeder,
        IOptions<DatabaseInitializationOptions> options) : IDatabaseInitializer
    {
        private readonly OrderManagementDbContext _dbContext = dbContext;
        private readonly DemoDataSeeder _demoDataSeeder = demoDataSeeder;
        private readonly DatabaseInitializationOptions _options = options.Value;

        public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.Database.MigrateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Datenbankmigration fehlgeschlagen: {ex.Message}");
            }

            if (!_options.SeedDemoData)
            {
                return Result.Success();
            }

            IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _demoDataSeeder.SeedAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                string detail = ex.InnerException?.Message ?? ex.Message;
                return Result.Fail($"Demo-Daten konnten nicht angelegt werden: {detail}");
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
