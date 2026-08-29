using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Testcontainers.MsSql;

namespace OrderManagement.Infrastructure.IntegrationTests
{
    [TestClass]
    public static class AssemblySetup
    {
        private static MsSqlContainer? _container;

        internal static string MasterConnectionString { get; private set; } = default!;

        [AssemblyInitialize]
        public static async Task AssemblyInitialize(TestContext _)
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

        [AssemblyCleanup]
        public static async Task AssemblyCleanup()
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
    }
}
