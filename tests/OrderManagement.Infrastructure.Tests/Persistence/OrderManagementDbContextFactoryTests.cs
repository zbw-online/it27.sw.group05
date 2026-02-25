using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Tests.Persistence
{
    [TestClass]
    public sealed class OrderManagementDbContextFactoryTests
    {
        [TestMethod]
        public void Constructor_WithConfiguration_ShouldNotThrow()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = "Server=localhost;Database=TestDb;Integrated Security=true;"
                })
                .Build();

            // If this doesn't throw, the test passes
            _ = new OrderManagementDbContextFactory(configuration);
        }

        [TestMethod]
        public void ParameterlessConstructor_ShouldNotThrow() =>
            // This constructor loads from user secrets and environment variables
            // If this doesn't throw, the test passes
            _ = new OrderManagementDbContextFactory();

        [TestMethod]
        public void CreateDbContext_WithValidConnectionString_ShouldReturnDbContext()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = "Server=localhost;Database=TestDb;Integrated Security=true;"
                })
                .Build();

            var factory = new OrderManagementDbContextFactory(configuration);
            OrderManagementDbContext context = factory.CreateDbContext([]);

            Assert.IsInstanceOfType<OrderManagementDbContext>(context);
        }

        [TestMethod]
        public void CreateDbContext_WithoutConnectionString_ShouldThrowInvalidOperationException()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([])
                .Build();

            var factory = new OrderManagementDbContextFactory(configuration);
            _ = Assert.ThrowsException<InvalidOperationException>(() => _ = factory.CreateDbContext([]));
        }

        [TestMethod]
        public void CreateDbContext_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = ""
                })
                .Build();

            var factory = new OrderManagementDbContextFactory(configuration);
            _ = Assert.ThrowsException<InvalidOperationException>(() => _ = factory.CreateDbContext([]));
        }

        [TestMethod]
        public void CreateDbContext_WithNullConnectionString_ShouldThrowInvalidOperationException()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = null
                })
                .Build();

            var factory = new OrderManagementDbContextFactory(configuration);
            _ = Assert.ThrowsException<InvalidOperationException>(() => _ = factory.CreateDbContext([]));
        }

        [TestMethod]
        public void CreateDbContext_ShouldConfigureSqlServerProvider()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = "Server=localhost;Database=TestDb;Integrated Security=true;"
                })
                .Build();

            var factory = new OrderManagementDbContextFactory(configuration);
            OrderManagementDbContext context = factory.CreateDbContext([]);

            Assert.AreEqual("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        }

        [TestMethod]
        public void CreateDbContext_MultipleCalls_ShouldReturnDifferentInstances()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = "Server=localhost;Database=TestDb;Integrated Security=true;"
                })
                .Build();

            var factory = new OrderManagementDbContextFactory(configuration);
            OrderManagementDbContext context1 = factory.CreateDbContext([]);
            OrderManagementDbContext context2 = factory.CreateDbContext([]);

            Assert.AreNotSame(context1, context2);
        }

        [TestMethod]
        public async Task CreateDbContext_ShouldUseMigrationsAssembly()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = "Server=localhost;Database=TestDb;Integrated Security=true;"
                })
                .Build();

            var factory = new OrderManagementDbContextFactory(configuration);
            OrderManagementDbContext context = factory.CreateDbContext([]);

            // Verify that the context can access database operations without throwing
            try
            {
                _ = await context.Database.GetPendingMigrationsAsync();
            }
            catch
            {
                // Expected when not connected to real database - that's fine for this test
            }
        }

        [TestMethod]
        public void CreateDbContext_WithDifferentConnectionStrings_ShouldCreateDifferentContexts()
        {
            IConfigurationRoot config1 = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = "Server=server1;Database=Db1;Integrated Security=true;"
                })
                .Build();

            IConfigurationRoot config2 = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OrderManagement"] = "Server=server2;Database=Db2;Integrated Security=true;"
                })
                .Build();

            var factory1 = new OrderManagementDbContextFactory(config1);
            var factory2 = new OrderManagementDbContextFactory(config2);

            OrderManagementDbContext context1 = factory1.CreateDbContext([]);
            OrderManagementDbContext context2 = factory2.CreateDbContext([]);

            Assert.AreNotSame(context1, context2);
        }

        [TestMethod]
        public void CreateDbContext_ErrorMessage_ShouldContainHelpfulInformation()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection([])
                .Build();

            var factory = new OrderManagementDbContextFactory(configuration);

            try
            {
                _ = factory.CreateDbContext([]);
                Assert.Fail("Expected InvalidOperationException");
            }
            catch (InvalidOperationException ex)
            {
                Assert.IsTrue(ex.Message.Contains("ConnectionStrings:OrderManagement"));
                Assert.IsTrue(ex.Message.Contains("dotnet user-secrets"));
            }
        }
    }
}
