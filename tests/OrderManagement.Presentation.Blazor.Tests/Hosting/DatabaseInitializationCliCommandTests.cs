using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Infrastructure.Persistence.Initialization;
using OrderManagement.Presentation.Blazor.Hosting;

using SharedKernel.Primitives;

namespace OrderManagement.Presentation.Blazor.Tests.Hosting
{
    [TestClass]
    public sealed class DatabaseInitializationCliCommandTests
    {
        [TestMethod]
        public async Task RunAsync_WhenInitializationSucceeds_ReturnsTrue()
        {
            bool result = await RunWithFakeInitializerAsync(Result.Success());

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task RunAsync_WhenInitializationFails_ReturnsFalse()
        {
            bool result = await RunWithFakeInitializerAsync(Result.Fail("Datenbank nicht erreichbar."));

            Assert.IsFalse(result);
        }

        private static async Task<bool> RunWithFakeInitializerAsync(Result initializerResult)
        {
            var services = new ServiceCollection();
            _ = services.AddScoped<IDatabaseInitializer>(_ => new FakeDatabaseInitializer(initializerResult));

            await using ServiceProvider provider = services.BuildServiceProvider();

            return await DatabaseInitializationCliCommand.RunAsync(provider);
        }

        private sealed class FakeDatabaseInitializer(Result result) : IDatabaseInitializer
        {
            public Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(result);
        }
    }
}
