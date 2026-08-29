using Microsoft.EntityFrameworkCore;

using OrderManagement.Infrastructure.Persistence;

using Reqnroll;

namespace OrderManagement.AcceptanceTests.Support
{
    [Binding]
    public sealed class DatabaseHooks(OrderManagementDbContext dbContext)
    {
        private readonly OrderManagementDbContext _dbContext = dbContext;

        [BeforeScenario]
        public async Task MigrateDatabaseAsync() => await _dbContext.Database.MigrateAsync();

        [AfterScenario]
        public async Task CleanupDatabaseAsync() => await _dbContext.Database.EnsureDeletedAsync();
    }
}
