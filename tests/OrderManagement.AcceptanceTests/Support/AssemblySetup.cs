using Microsoft.Data.SqlClient;

using Reqnroll;

using Testcontainers.MsSql;

namespace OrderManagement.AcceptanceTests.Support
{
    [Binding]
    public sealed class AssemblySetup
    {
        private static MsSqlContainer? _container;

        internal static string MasterConnectionString { get; private set; } = default!;

        [BeforeTestRun]
        public static async Task BeforeTestRunAsync()
        {
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Test@1234!")
                .Build();

            await _container.StartAsync();

            var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = "master",
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            MasterConnectionString = builder.ConnectionString;
        }

        [AfterTestRun]
        public static async Task AfterTestRunAsync()
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
    }
}
