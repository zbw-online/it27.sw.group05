using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;

namespace OrderManagement.TestSupport
{
    public sealed class SqlServerTestContainer : IAsyncDisposable
    {
        public const string ImageTag = "mcr.microsoft.com/mssql/server:2022-CU26-ubuntu-22.04";

        private const string Password = "Test@1234!";
        private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

        public const string ExternalServerEnvironmentVariable = "ORDERMANAGEMENT_TEST_SQLSERVER";

        private MsSqlContainer? _container;

        public string MasterConnectionString { get; private set; } = default!;

        public async Task StartAsync()
        {
            string? externalConnectionString = Environment.GetEnvironmentVariable(ExternalServerEnvironmentVariable);

            if (!string.IsNullOrWhiteSpace(externalConnectionString))
            {
                await UseExternalServerAsync(externalConnectionString);
                return;
            }

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

        private async Task UseExternalServerAsync(string externalConnectionString)
        {
            var builder = new SqlConnectionStringBuilder(externalConnectionString)
            {
                InitialCatalog = "master",
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            // Never surface the raw connection string (it carries the password) in an error message.
            string serverDescription = builder.DataSource;

            try
            {
                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Could not reach the external SQL Server '{serverDescription}' configured via " +
                    $"{ExternalServerEnvironmentVariable}. Verify the server is running and reachable.",
                    ex);
            }

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
