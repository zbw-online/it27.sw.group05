using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Infrastructure.Persistence;

using Testcontainers.MsSql;

namespace OrderManagement.Infrastructure.Tests
{
    [TestClass]
    public static class AssemblySetup
    {
        internal static MsSqlContainer? MsSqlContainer { get; private set; }
        internal static string? ConnectionString { get; private set; }

        [AssemblyInitialize]
        public static async Task AssemblyInitialize(TestContext testContext)
        {
            TestContext x = testContext; // Avoid "unused parameter" warning
            MsSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Test@1234!")
                .Build();

            await MsSqlContainer.StartAsync();
            ConnectionString = MsSqlContainer.GetConnectionString();

            // Create schema once using a temporary context
            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            await using var setupContext = new OrderManagementDbContext(options);
            _ = await setupContext.Database.EnsureCreatedAsync();
        }

        [AssemblyCleanup]
        public static async Task AssemblyCleanup()
        {
            if (MsSqlContainer is not null)
            {
                await MsSqlContainer.DisposeAsync();
            }
        }
    }
}
