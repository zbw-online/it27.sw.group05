using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;

namespace OrderManagement.TestSupport
{
    public sealed class SqlServerTestContainer : IAsyncDisposable
    {
        public const string ImageTag = "mcr.microsoft.com/mssql/server:2022-CU26-ubuntu-22.04";

        private const string Password = "Test@1234!";
        private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

        private MsSqlContainer? _container;

        public string MasterConnectionString { get; private set; } = default!;

        public async Task StartAsync()
        {
            _container = new MsSqlBuilder(ImageTag)
                .WithPassword(Password)
                .Build();

            try
            {
                using var startupCts = new CancellationTokenSource(StartupTimeout);
                await _container.StartAsync(startupCts.Token);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to start the SQL Server Testcontainer. Make sure Docker is running and reachable " +
                    "(this project's tests require Docker; see README for the Testcontainers prerequisite).",
                    ex);
            }

            var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = "master",
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            MasterConnectionString = builder.ConnectionString;
        }

        public async ValueTask DisposeAsync()
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
    }
}
