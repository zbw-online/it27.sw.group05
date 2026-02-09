using Microsoft.Extensions.Configuration;

using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Tests.Persistence
{
    [TestClass]
    public sealed class OrderManagementDbContextFactoryTests
    {
        [TestMethod]
        public void CreateDbContextThrowsWhenConnectionStringMissing()
        {
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            var factory = new OrderManagementDbContextFactory(config);

            _ = Assert.ThrowsException<InvalidOperationException>(
                () => factory.CreateDbContext([]));
        }

        [TestMethod]
        public void CreateDbContextUsesProvidedConnectionString()
        {
            KeyValuePair<string, string?>[] initialData =
[
    new KeyValuePair<string, string?>(
        "ConnectionStrings:OrderManagement",
        "Server=(localdb)\\mssqllocaldb;Database=OrderManagement.Tests;Trusted_Connection=True;")
];

            IConfigurationRoot inMemoryConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(initialData)
                .Build();


            var factory = new OrderManagementDbContextFactory(inMemoryConfig);

            using OrderManagementDbContext context = factory.CreateDbContext([]);

        }


    }
}
