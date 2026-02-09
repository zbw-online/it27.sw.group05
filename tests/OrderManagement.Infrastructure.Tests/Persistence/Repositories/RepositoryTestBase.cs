using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories
{
    public abstract class RepositoryTestBase : IDisposable
    {
        protected OrderManagementDbContext Context { get; }
        private readonly SqliteConnection _connection;

        protected RepositoryTestBase()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new OrderManagementDbContext(options);
            _ = Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Close();
            _connection.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
