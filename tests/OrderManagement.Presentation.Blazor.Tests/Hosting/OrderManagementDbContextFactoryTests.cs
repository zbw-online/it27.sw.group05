using Microsoft.Extensions.Configuration;

using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Presentation.Blazor.Hosting;

namespace OrderManagement.Presentation.Blazor.Tests.Hosting
{
    [TestClass]
    public sealed class OrderManagementDbContextFactoryTests
    {
        [TestMethod]
        public void Constructor_WithConfiguration_ShouldNotThrow()
        {
            IConfigurationRoot configuration = CreateConfiguration("Server=localhost;Database=TestDb;Integrated Security=true;TrustServerCertificate=True");

            _ = new OrderManagementDbContextFactory(configuration);
        }

        [TestMethod]
        public void CreateDbContext_WithValidConnectionString_ShouldReturnSqlServerContext()
        {
            IConfigurationRoot configuration = CreateConfiguration("Server=localhost;Database=TestDb;Integrated Security=true;TrustServerCertificate=True");
            var factory = new OrderManagementDbContextFactory(configuration);

            using OrderManagementDbContext context = factory.CreateDbContext([]);

            Assert.IsInstanceOfType<OrderManagementDbContext>(context);
            Assert.AreEqual("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        }

        [TestMethod]
        public void CreateDbContext_WithoutConnectionString_ShouldThrowHelpfulInvalidOperationException()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([])
                .Build();

            var factory = new OrderManagementDbContextFactory(configuration);

            InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => _ = factory.CreateDbContext([]));

            StringAssert.Contains(ex.Message, "ConnectionStrings:OrderManagement");
            StringAssert.Contains(ex.Message, "dotnet user-secrets");
        }

        [TestMethod]
        public void CreateDbContext_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
        {
            IConfigurationRoot configuration = CreateConfiguration(string.Empty);
            var factory = new OrderManagementDbContextFactory(configuration);

            _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = factory.CreateDbContext([]));
        }

        private static IConfigurationRoot CreateConfiguration(string? connectionString)
            => new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = connectionString
                })
                .Build();
    }
}
